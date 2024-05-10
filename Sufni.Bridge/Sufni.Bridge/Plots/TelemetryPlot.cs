using ScottPlot;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Plots;

public class TelemetryPlot(Plot plot) : SufniPlot(plot)
{
    protected Color FrontColor = Color.FromHex("#3288bd");
    protected Color RearColor = Color.FromHex("#66c2a5");
    
    public virtual void LoadTelemetryData(TelemetryData telemetryData) { }
}