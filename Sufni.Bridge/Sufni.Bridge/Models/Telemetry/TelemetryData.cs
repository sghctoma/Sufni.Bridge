using System.Collections.Generic;
using System.Linq;
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

public record TravelStatistics(double Max, double Average, int Bottomouts);

public record VelocityStatistics(
    double AverageRebound,
    double MaxRebound,
    double AverageCompression,
    double MaxCompression);

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

    public double CalculateBalance()
    {
        return 0;
        // TODO: implement
    }
};