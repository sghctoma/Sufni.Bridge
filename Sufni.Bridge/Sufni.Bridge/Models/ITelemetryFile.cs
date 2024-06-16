using System;
using System.Threading.Tasks;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Models;

public interface ITelemetryFile
{
    public string Name { get; set; }
    public string FileName { get; }
    public bool ShouldBeImported { get; set; }
    public bool Imported { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; }
    public string Duration { get; }

    public Task<byte[]> GeneratePsstAsync(Linkage linkage, Calibration? frontCal, Calibration? rearCal);
    public Task OnImported();
}