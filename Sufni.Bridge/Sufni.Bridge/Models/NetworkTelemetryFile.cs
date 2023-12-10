using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sufni.Bridge.Models.Telemetry;

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

    private readonly IPEndPoint ipEndPoint;
    
    public async Task<byte[]> GeneratePsstAsync(Linkage linkage, Calibration? frontCal, Calibration? rearCal)
    {
        var idString = FileName[..5].TrimStart('0');
        var idUint = int.Parse(idString);
        var rawData = await SstTcpClient.GetFile(ipEndPoint, idUint);
        var rawTelemetryData = new RawTelemetryData(rawData);
        var telemetryData = new TelemetryData(FileName,
            rawTelemetryData.Version, rawTelemetryData.SampleRate, rawTelemetryData.Timestamp,
            frontCal, rearCal, linkage);
        return telemetryData.ProcessRecording(rawTelemetryData.Front, rawTelemetryData.Rear);
    }
    
    public async Task<byte[]> GeneratePsstAsync(byte[] linkage, byte[] calibrations)
    {
        var idString = FileName[..5].TrimStart('0');
        var idUint = int.Parse(idString);
        var rawData = await SstTcpClient.GetFile(ipEndPoint, idUint);
        var psst = ITelemetryFile.GeneratePsstNative(
            rawData, rawData.Length, 
            linkage, linkage.Length,
            calibrations, calibrations.Length);
        if (psst.DataSize < 0)
        {
            throw new Exception("SST => PSST conversion failed.");
        }

        var psstBytes = new byte[psst.DataSize];
        Marshal.Copy(psst.DataPointer, psstBytes, 0, psst.DataSize);

        return psstBytes;
    }

    public void OnImported()
    {
        Imported = true;
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
        ipEndPoint = source;
    }
}