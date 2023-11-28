using System;
using System.Net;

namespace Sufni.Bridge.Models;

public class NetworkTelemetryFile : ITelemetryFile
{
    public string Name { get; set; }
    public string FileName { get; }
    public bool ShouldBeImported { get; set; }
    public bool Imported { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; init; }
    public string Duration { get; init; }
    public string Base64Data { get; }
    
    public byte[] GeneratePsst(byte[] linkage, byte[] calibrations)
    {
        throw new NotImplementedException();
    }

    public void OnImported()
    {
        throw new NotImplementedException();
    }
    
    public NetworkTelemetryFile(IPEndPoint source, ushort sampleRate, string name, ulong size, ulong timestamp)
    {
        var count = (size - 16 /* sizeof(header) */) / 4 /* sizeof(record) */;
        var duration = TimeSpan.FromSeconds((double)count / sampleRate);
        ShouldBeImported = duration.TotalSeconds >= 5;
        StartTime = DateTimeOffset.FromUnixTimeSeconds((int)timestamp).DateTime;
        Duration = duration.ToString(@"hh\:mm\:ss");
        Name = name;
        FileName = name;
        Description = $"Imported from {name}";
        Base64Data = "XXX";
    }
}