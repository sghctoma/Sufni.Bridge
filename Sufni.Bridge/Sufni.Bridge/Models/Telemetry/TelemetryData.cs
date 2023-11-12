using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using MessagePack;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618

namespace Sufni.Bridge.Models.Telemetry;

[MessagePackObject(keyAsPropertyName: true)]
public class Linkage
{
    public string Name { get; set; }
    public double HeadAngle { get; set; }
    public double MaxFrontStroke { get; set; }
    public double MaxRearStroke { get; set; }
    public double MaxFrontTravel { get; set; }
    public double MaxRearTravel { get; set; }
    public double[][] LeverageRatio { get; set; }
    public double[] ShockWheelCoeffs { get; set; }
};

[MessagePackObject(keyAsPropertyName: true)]
public class Calibration
{
    public string Name { get; set; }
    public int MethodId { get; set; }
    public Dictionary<string, double> Inputs { get; set; }
};

[MessagePackObject(keyAsPropertyName: true)]
public class StrokeStat
{
    public double SumTravel { get; set; }
    public double MaxTravel { get; set; }
    public double SumVelocity { get; set; }
    public double MaxVelocity { get; set; }
    public int Bottomouts { get; set; }
    public int Count { get; set; }
};

[MessagePackObject(keyAsPropertyName: true)]
public class Stroke
{
    public int Start { get; set; }
    public int End { get; set; }
    public StrokeStat Stat { get; set; }
    public int[] DigitizedTravel { get; set; }
    public int[] DigitizedVelocity { get; set; }
    public int[] FineDigitizedVelocity { get; set; }
};

[MessagePackObject(keyAsPropertyName: true)]
public class Strokes
{
    public Stroke[] Compressions { get; set; }
    public Stroke[] Rebounds { get; set; }
};

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
    public Calibration Calibration { get; set; }
    public double[] Travel { get; set; }
    public double[] Velocity { get; set; }
    public Strokes Strokes { get; set; }
    public double[] TravelBins { get; set; }
    public double[] VelocityBins { get; set; }
    public double[] FineVelocityBins { get; set; }
};

public record HistogramData(List<double> Bins, List<double> Values);

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
    public string Name { get; set; }
    public int Version { get; set; }
    public int SampleRate { get; set; }
    public int Timestamp { get; set; }
    public Suspension Front { get; set; }
    public Suspension Rear { get; set; }
    public Linkage Linkage { get; set; }
    public Airtime[] Airtimes { get; set; }
    
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
            suspension.TravelBins.ToList().GetRange(0, suspension.TravelBins.Length),
            hist.ToList());
    }
    
    public HistogramData CalculateVelocityHistogram(SuspensionType type)
    {
        var suspension = type == SuspensionType.Front ? Front : Rear;

        var hist = new double[suspension.VelocityBins.Length - 1];
        var totalCount = 0;

        foreach (var s in suspension.Strokes.Compressions.Concat(suspension.Strokes.Rebounds))
        {
            totalCount += s.Stat.Count;
            foreach (var d in s.DigitizedVelocity)
            {
                hist[d] += 1;
            }
        }

        hist = hist.Select(value => value / totalCount * 100.0).ToArray();

        return new HistogramData(
            suspension.VelocityBins.ToList().GetRange(0, suspension.VelocityBins.Length),
            hist.ToList());
    }

    public NormalDistributionData CalculateNormalDistribution(SuspensionType type)
    {
        var suspension = type == SuspensionType.Front ? Front : Rear;
        var step = suspension.VelocityBins[1] - suspension.VelocityBins[0];
        var velocity = suspension.Velocity.ToList();
        
        var strokeVelocity = new List<double>();
        foreach (var s in suspension.Strokes.Compressions.Concat(suspension.Strokes.Rebounds))
        {
            strokeVelocity.AddRange(velocity.GetRange(s.Start, s.End - s.Start + 1));
        }

        var strokeVelocityArray = strokeVelocity.ToArray();
        var mu = strokeVelocity.Mean();
        var std = strokeVelocity.StandardDeviation();

        var ny = Enumerable.Range(0, 100)
            .Select(i => strokeVelocityArray.Min() + i * (strokeVelocityArray.Max() - strokeVelocityArray.Min()) / 99)
            .ToArray();

        var pdf = ny.Select(value => Normal.PDF(mu, std, value) * step * 100).ToList();

        return new NormalDistributionData(ny.ToList(), pdf);
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

        var totalCount = 0;
        
        var lsc = 0.0;
        var hsc = 0.0;
        foreach (var compression in suspension.Strokes.Compressions)
        {
            totalCount += compression.Stat.Count;
            var strokeLsc = suspension.Velocity
                .Skip(compression.Start)
                .Take(compression.End - compression.Start + 1)
                .Count(v => v < highSpeedThreshold);
            lsc += strokeLsc;
            hsc += compression.Stat.Count - strokeLsc;
        }
        
        var lsr = 0.0;
        var hsr = 0.0;
        foreach (var rebound in suspension.Strokes.Rebounds)
        {
            totalCount += rebound.Stat.Count;
            var strokeLsr = suspension.Velocity
                .Skip(rebound.Start)
                .Take(rebound.End - rebound.Start + 1)
                .Count(v => v > -highSpeedThreshold);
            lsr += strokeLsr;
            hsr += rebound.Stat.Count - strokeLsr;
        }

        return new VelocityBands(
            lsc / totalCount * 100.0,
            hsc / totalCount * 100.0,
            lsr / totalCount * 100.0,
            hsr / totalCount * 100.0);
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
            frontTravelVelocity.Item1.ToList(),
            frontTravelVelocity.Item2.ToList(),
            frontTravelVelocity.Item1.Select(t => frontPoly(t)).ToList(),
            rearTravelVelocity.Item1.ToList(),
            rearTravelVelocity.Item2.ToList(),
            rearTravelVelocity.Item1.Select(t => rearPoly(t)).ToList(),
            msd);
    }
};