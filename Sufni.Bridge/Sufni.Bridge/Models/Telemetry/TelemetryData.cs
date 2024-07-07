using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using MessagePack;
using Generate = ScottPlot.Generate;

#pragma warning disable CS8618

namespace Sufni.Bridge.Models.Telemetry;

[MessagePackObject(keyAsPropertyName: true)]
public class Airtime
{
    public double Start { get; set; }
    public double End { get; set; }
};

[MessagePackObject(keyAsPropertyName: true)]
public class Suspension
{
    public bool Present { get; set; }
    public Calibration? Calibration { get; set; }
    public double[] Travel { get; set; }
    public double[] Velocity { get; set; }
    public Strokes Strokes { get; set; }
    public double[] TravelBins { get; set; }
    public double[] VelocityBins { get; set; }
    public double[] FineVelocityBins { get; set; }
};

public record HistogramData(List<double> Bins, List<double> Values);
public record StackedHistogramData(List<double> Bins, List<double[]> Values);

public record TravelStatistics(double Max, double Average, int Bottomouts);

public record VelocityStatistics(
    double AverageRebound,
    double MaxRebound,
    double AverageCompression,
    double MaxCompression);

public record NormalDistributionData(
    List<double> Y,
    List<double> Pdf);

public record VelocityBands(
    double LowSpeedCompression,
    double HighSpeedCompression,
    double LowSpeedRebound,
    double HighSpeedRebound);

public enum SuspensionType
{
    Front,
    Rear
}

public enum BalanceType
{
    Compression,
    Rebound
}

public record BalanceData(
    List<double> FrontTravel,
    List<double> FrontVelocity,
    List<double> FrontTrend,
    List<double> RearTravel,
    List<double> RearVelocity,
    List<double> RearTrend,
    double MeanSignedDeviation);

[MessagePackObject(keyAsPropertyName: true)]
public class TelemetryData
{
    public const int TravelBinsForVelocityHistogram = 10;
        
    #region Public properties

    public string Name { get; set; }
    public int Version { get; set; }
    public int SampleRate { get; set; }
    public int Timestamp { get; set; }
    public Suspension Front { get; set; }
    public Suspension Rear { get; set; }
    public Linkage Linkage { get; set; }
    public Airtime[] Airtimes { get; set; }

    #endregion

    #region Constructors

    public TelemetryData() { }

    public TelemetryData(string name, int version, int sampleRate, int timestamp,
        Calibration? frontCal, Calibration? rearCal, Linkage linkage)
    {
        Name = name;
        Version = version;
        SampleRate = sampleRate;
        Timestamp = timestamp;
        Linkage = linkage;

        Front = new Suspension
        {
            Calibration = frontCal,
            Strokes = new Strokes()
        };

        Rear = new Suspension
        {
            Calibration = rearCal,
            Strokes = new Strokes()
        };
    }

    #endregion
    
    #region Private helpers for ProcessRecording

    private static double[] Linspace(double min, double max, int num)
    {
        var step = (max - min) / (num - 1);
        var bins = new double[num];
        
        for (var i = 0; i < num; i++)
        {
            bins[i] = min + step * i;
        }

        return bins;
    }
    
    private static int[] Digitize(double[] data, double[] bins)
    {
        var inds = new int[data.Length];
        for (var k = 0; k < data.Length; k++)
        {
            var i = Array.BinarySearch(bins, data[k]);
            if (i < 0) i = ~i;
            // If current value is not exactly a bin boundary, we subtract 1 to make
            // the digitized slice indexed from 0 instead of 1. We do the same if a
            // value would exceed existing bins.
            if (data[k] >= bins[^1] || Math.Abs(data[k] - bins[i]) > 0.0001)
            {
                i -= 1;
            }
            inds[k] = i;
        }
        return inds;
    }

    private void CalculateAirTimes()
    {
        var airtimes  = new List<Airtime>();

        if (Front.Present && Rear.Present)
        {
            foreach (var f in Front.Strokes.Idlings)
            {
                if (!f.AirCandidate) continue;
                foreach (var r in Rear.Strokes.Idlings)
                {
                    if (!r.AirCandidate || !f.Overlaps(r)) continue;
                    f.AirCandidate = false;
                    r.AirCandidate = false;

                    var at = new Airtime
                    {
                        Start = Math.Min(f.Start, r.Start) / (double)SampleRate,
                        End = Math.Min(f.End, r.End) / (double)SampleRate
                    };
                    airtimes.Add(at);
                    break;
                }
            }

            var maxMean = (Linkage.MaxFrontTravel + Linkage.MaxRearTravel) / 2.0;

            foreach (var f in Front.Strokes.Idlings)
            {
                if (!f.AirCandidate) continue;
                var fMean = Front.Travel[f.Start..(f.End + 1)].Mean();
                var rMean = Rear.Travel[f.Start..(f.End + 1)].Mean();

                if (!((fMean + rMean) / 2 <= maxMean * Parameters.AirtimeTravelMeanThresholdRatio)) continue;
                var at = new Airtime
                {
                    Start = f.Start / (double)SampleRate,
                    End = f.End / (double)SampleRate
                };
                airtimes.Add(at);
            }

            foreach (var r in Rear.Strokes.Idlings)
            {
                if (!r.AirCandidate) continue;
                var fMean = Front.Travel[r.Start..(r.End + 1)].Mean();
                var rMean = Rear.Travel[r.Start..(r.End + 1)].Mean();

                if (!((fMean + rMean) / 2 <= maxMean * Parameters.AirtimeTravelMeanThresholdRatio)) continue;
                var at = new Airtime
                {
                    Start = r.Start / (double)SampleRate,
                    End = r.End / (double)SampleRate
                };
                airtimes.Add(at);
            }
        }
        else if (Front.Present)
        {
            foreach (var f in Front.Strokes.Idlings)
            {
                if (!f.AirCandidate) continue;
                var at = new Airtime
                {
                    Start = f.Start / (double)SampleRate,
                    End = f.End / (double)SampleRate
                };
                airtimes.Add(at);
            }
        }
        else if (Rear.Present)
        {
            foreach (var r in Rear.Strokes.Idlings)
            {
                if (!r.AirCandidate) continue;
                var at = new Airtime
                {
                    Start = r.Start / (double)SampleRate,
                    End = r.End / (double)SampleRate
                };
                airtimes.Add(at);
            }
        }

        Airtimes = [.. airtimes];
    }

    private static (double[], int[]) DigitizeVelocity(double[] v, double step)
    {
        // Subtracting half bin ensures that 0 will be at the middle of one bin
        var mn = (Math.Floor(v.Min() / step) - 0.5) * step;
        // Adding 1.5 bins ensures that all values will fit in bins, and that the last bin fits the step boundary.
        var mx = (Math.Floor(v.Max() / step) + 1.5) * step;
        var bins = Linspace(mn, mx, (int)((mx - mn) / step) + 1);
        var data = Digitize(v, bins);
        return (bins, data);
    }
    
    #endregion
    
    #region PSST conversion

    public byte[] ProcessRecording(ushort[] front, ushort[] rear)
    {
        // Evaluate front and rear input arrays
        var fc = front.Length;
        var rc = rear.Length;
        Front.Present = fc != 0;
        Rear.Present = rc != 0;
        if (!Front.Present && !Rear.Present)
        {
            throw new Exception("Front and rear record arrays are empty!");
        }
        if (Front.Present && Rear.Present && fc != rc)
        {
            throw new Exception("Front and rear record counts are not equal!");
        }
        
        // Create time array
        var recordCount = Math.Max(fc, rc);
        var time = new double[recordCount];
        for (var i = 0; i < time.Length; i++)
        {
            time[i] = 1.0 / SampleRate * i;
        }

        // Create Savitzky-Golay filter to get smoothed velocity data
        var filter = new SavitzkyGolay(51, 1, 3);

        if (Front.Present)
        {
            Front.Travel = new double[fc];
            var frontCoeff = Math.Sin(Linkage.HeadAngle * Math.PI / 180.0);

            for (var i = 0; i < front.Length; i++)
            {
                // Front travel might under/overshoot because of erroneous data
                // acquisition. Errors might occur mid-ride (e.g. broken electrical
                // connection due to vibration), so we don't error out, just cap
                // travel. Errors like these will be obvious on the graphs, and
                // the affected regions can be filtered by hand.
                var travel = Front.Calibration!.Evaluate(front[i]);
                var x = travel * frontCoeff;
                x = Math.Max(0, x);
                x = Math.Min(x, Linkage.MaxFrontTravel);
                Front.Travel[i] = x;
            }

            var tbins = Linspace(0, Linkage.MaxFrontTravel, Parameters.TravelHistBins + 1);
            var dt = Digitize(Front.Travel, tbins);
            Front.TravelBins = tbins;

            var v = filter.Process(Front.Travel, time);
            Front.Velocity = v;
            var (vbins, dv) = DigitizeVelocity(v, Parameters.VelocityHistStep);
            Front.VelocityBins = vbins;
            var (vbinsFine, dvFine) = DigitizeVelocity(v, Parameters.VelocityHistStepFine);
            Front.FineVelocityBins = vbinsFine;

            var strokes = Strokes.FilterStrokes(v, Front.Travel, Linkage.MaxFrontTravel, SampleRate);
            Front.Strokes.Categorize(strokes);
            if (Front.Strokes.Compressions.Length == 0 && Front.Strokes.Rebounds.Length == 0)
            {
                Front.Present = false;
            } else
            {
                Front.Strokes.Digitize(dt, dv, dvFine);
            }
        }
        
        if (Rear.Present)
        {
            Rear.Travel = new double[rc];

            for (var i = 0; i < rear.Length; i++)
            {
                // Rear travel might also overshoot the max because of
                //  a) inaccurately measured leverage ratio
                //  b) inaccuracies introduced by polynomial fitting
                // So we just cap it at calculated maximum.
                var travel = Rear.Calibration!.Evaluate(rear[i]);
                var x = Linkage.Polynomial.Evaluate(travel);
                x = Math.Max(0, x);
                x = Math.Min(x, Linkage.MaxRearTravel);
                Rear.Travel[i] = x;
            }
            
            var tbins = Linspace(0, Linkage.MaxRearTravel, Parameters.TravelHistBins + 1);
            var dt = Digitize(Rear.Travel, tbins);
            Rear.TravelBins = tbins;

            var v = filter.Process(Rear.Travel, time);
            Rear.Velocity = v;
            var (vbins, dv) = DigitizeVelocity(v, Parameters.VelocityHistStep);
            Rear.VelocityBins = vbins;
            var (vbinsFine, dvFine) = DigitizeVelocity(v, Parameters.VelocityHistStepFine);
            Rear.FineVelocityBins = vbinsFine;

            var strokes = Strokes.FilterStrokes(v, Rear.Travel, Linkage.MaxRearTravel, SampleRate);
            Rear.Strokes.Categorize(strokes);
            if (Rear.Strokes.Compressions.Length == 0 && Rear.Strokes.Rebounds.Length == 0)
            {
                Rear.Present = false;
            } else
            {
                Rear.Strokes.Digitize(dt, dv, dvFine);
            }
        }
        
        CalculateAirTimes();

        return MessagePackSerializer.Serialize(this);
    }

    #endregion

    #region Data calculations

    public HistogramData CalculateTravelHistogram(SuspensionType type)
    {
        var suspension = type == SuspensionType.Front ? Front : Rear;

        var hist = new double[suspension.TravelBins.Length - 1];
        var totalCount = 0;

        foreach (var s in suspension.Strokes.Compressions.Concat(suspension.Strokes.Rebounds))
        {
            totalCount += s.Stat.Count;
            foreach (var d in s.DigitizedTravel)
            {
                hist[d] += 1;
            }
        }

        hist = hist.Select(value => value / totalCount * 100.0).ToArray();

        return new HistogramData(
            suspension.TravelBins.ToList().GetRange(0, suspension.TravelBins.Length), [.. hist]);
    }

    public StackedHistogramData CalculateVelocityHistogram(SuspensionType type)
    {
        var suspension = type == SuspensionType.Front ? Front : Rear;

        var divider = (suspension.TravelBins.Length - 1) / TravelBinsForVelocityHistogram;
        var hist = new double[suspension.VelocityBins.Length - 1][];
        for (var i = 0; i < hist.Length; i++)
        {
            hist[i] = Generate.Zeros(TravelBinsForVelocityHistogram);
        }
        
        var totalCount = 0;
        foreach (var s in suspension.Strokes.Compressions.Concat(suspension.Strokes.Rebounds))
        {
            totalCount += s.Stat.Count;
            for (int i = 0; i < s.Stat.Count; ++i)
            {
                var vbin = s.DigitizedVelocity[i];
                var tbin = s.DigitizedTravel[i] / divider;
                hist[vbin][tbin] += 1;
            }
        }

        var largestBin = 0.0;
        foreach (var travelHist in hist)
        {
            var travelSum = 0.0;
            for (var j = 0; j < TravelBinsForVelocityHistogram; j++)
            {
                travelHist[j] = travelHist[j] / totalCount * 100.0;
                travelSum += travelHist[j];
            }

            largestBin = Math.Max(travelSum, largestBin);
        }
        
        return new StackedHistogramData(
            suspension.VelocityBins.ToList().GetRange(0, suspension.VelocityBins.Length), [.. hist]);
    }

    public NormalDistributionData CalculateNormalDistribution(SuspensionType type)
    {
        var suspension = type == SuspensionType.Front ? Front : Rear;
        var step = suspension.VelocityBins[1] - suspension.VelocityBins[0];
        var velocity = suspension.Velocity.ToList();

        var strokeVelocity = new List<double>();
        foreach (var s in suspension.Strokes.Compressions)
        {
            strokeVelocity.AddRange(velocity.GetRange(s.Start, s.End - s.Start + 1));
        }
        foreach (var s in suspension.Strokes.Rebounds)
        {
            strokeVelocity.AddRange(velocity.GetRange(s.Start, s.End - s.Start + 1));
        }

        var mu = strokeVelocity.Mean();
        var std = strokeVelocity.StandardDeviation();

        var min = strokeVelocity.Min();
        var max = strokeVelocity.Max();
        var range = max - min;
        var ny = new double[100];
        for (int i = 0; i < 100; i++)
        {
            ny[i] = min + i * range / 99;
        }

        var pdf = new List<double>(100);
        for (int i = 0; i < 100; i++)
        {
            pdf.Add(Normal.PDF(mu, std, ny[i]) * step * 100);
        }

        return new NormalDistributionData([.. ny], pdf);
    }

    public TravelStatistics CalculateTravelStatistics(SuspensionType type)
    {
        var suspension = type == SuspensionType.Front ? Front : Rear;
        
        var sum = 0.0;
        var count = 0.0;
        var mx = 0.0;
        var bo = 0;
        
        foreach (var stroke in suspension.Strokes.Compressions.Concat(suspension.Strokes.Rebounds))
        {
            sum += stroke.Stat.SumTravel;
            count += stroke.Stat.Count;
            bo += stroke.Stat.Bottomouts;
            if (stroke.Stat.MaxTravel > mx)
            {
                mx = stroke.Stat.MaxTravel;
            }
        }

        return new TravelStatistics(mx, sum / count, bo);
    }

    public VelocityStatistics CalculateVelocityStatistics(SuspensionType type)
    {
        var suspension = type == SuspensionType.Front ? Front : Rear;

        var csum = 0.0;
        var ccount = 0.0;
        var maxc = 0.0;
        foreach (var compression in suspension.Strokes.Compressions)
        {
            csum += compression.Stat.SumVelocity;
            ccount += compression.Stat.Count;
            if (compression.Stat.MaxVelocity > maxc)
            {
                maxc = compression.Stat.MaxVelocity;
            }
        }
        var rsum = 0.0;
        var rcount = 0.0;
        var maxr = 0.0;
        foreach (var rebound in suspension.Strokes.Rebounds)
        {
            rsum += rebound.Stat.SumVelocity;
            rcount += rebound.Stat.Count;
            if (rebound.Stat.MaxVelocity < maxr)
            {
                maxr = rebound.Stat.MaxVelocity;
            }
        }

        return new VelocityStatistics(
            rsum / rcount, 
            maxr, 
            csum / ccount, 
            maxc);
    }

    public VelocityBands CalculateVelocityBands(SuspensionType type, double highSpeedThreshold)
    {
        var suspension = type == SuspensionType.Front ? Front : Rear;
        var velocity = suspension.Velocity;

        var totalCount = 0.0;
        var lsc = 0.0;
        var hsc = 0.0;

        // Process compressions
        foreach (var compression in suspension.Strokes.Compressions)
        {
            totalCount += compression.Stat.Count;
            for (int i = compression.Start; i <= compression.End; i++)
            {
                if (velocity[i] < highSpeedThreshold)
                {
                    lsc++;
                }
                else
                {
                    hsc++;
                }
            }
        }

        var lsr = 0.0;
        var hsr = 0.0;

        // Process rebounds
        foreach (var rebound in suspension.Strokes.Rebounds)
        {
            totalCount += rebound.Stat.Count;
            for (int i = rebound.Start; i <= rebound.End; i++)
            {
                if (velocity[i] > -highSpeedThreshold)
                {
                    lsr++;
                }
                else
                {
                    hsr++;
                }
            }
        }

        var totalPercentage = 100.0 / totalCount;
        return new VelocityBands(
            lsc * totalPercentage,
            hsc * totalPercentage,
            lsr * totalPercentage,
            hsr * totalPercentage);
    }
    
    private static Func<double, double> FitPolynomial(double[] x, double[] y)
    {
        var coefficients = Fit.Polynomial(x, y, 1);
        return t => coefficients[1] * t + coefficients[0];
    }
    
    private (double[], double[]) TravelVelocity(SuspensionType suspensionType, BalanceType balanceType)
    {
        var suspension = suspensionType == SuspensionType.Front ? Front : Rear;
        var travelMax = suspensionType == SuspensionType.Front ? Linkage.MaxFrontTravel : Linkage.MaxRearTravel;
        var strokes = balanceType == BalanceType.Compression
            ? suspension.Strokes.Compressions
            : suspension.Strokes.Rebounds;

        var t = new List<double>();
        var v = new List<double>();

        foreach (var s in strokes)
        {
            t.Add(s.Stat.MaxTravel / travelMax * 100);
            
            // Use positive values for rebound too, because ScottPlot can't invert axis easily. 
            v.Add(balanceType == BalanceType.Rebound ?  -s.Stat.MaxVelocity : s.Stat.MaxVelocity);
        }

        var  tArray = t.ToArray();
        var vArray = v.ToArray();

        Array.Sort(tArray, vArray);

        return (tArray, vArray);
    }

    public BalanceData CalculateBalance(BalanceType type)
    {
        var frontTravelVelocity = TravelVelocity(SuspensionType.Front, type);
        var rearTravelVelocity = TravelVelocity(SuspensionType.Rear, type);

        var frontPoly = FitPolynomial(frontTravelVelocity.Item1, frontTravelVelocity.Item2);
        var rearPoly = FitPolynomial(rearTravelVelocity.Item1, rearTravelVelocity.Item2);
        
        var frontTrend = frontTravelVelocity.Item1.Select(t => frontPoly(t)).ToList();
        var rearTrend = rearTravelVelocity.Item1.Select(t => rearPoly(t)).ToList();
        
        var sum = frontTrend.Zip(rearTrend, (fx, gx) => fx - gx).Sum();
        var msd = sum / frontTrend.Count;

        return new BalanceData(
            [.. frontTravelVelocity.Item1],
            [.. frontTravelVelocity.Item2],
            frontTravelVelocity.Item1.Select(t => frontPoly(t)).ToList(),
            [.. rearTravelVelocity.Item1],
            [.. rearTravelVelocity.Item2],
            rearTravelVelocity.Item1.Select(t => rearPoly(t)).ToList(),
            msd);
    }
    
    #endregion
};