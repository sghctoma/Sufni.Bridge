using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Models;

public class MassStorageTelemetryFile : ITelemetryFile
{
    private readonly FileInfo fileInfo;

    public string Name { get; set; }
    public string FileName => fileInfo.Name;
    public bool ShouldBeImported { get; set; }
    public bool Imported { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; init; }
    public string Duration { get; init; }
    
    public MassStorageTelemetryFile(FileInfo fileInfo)
    {
        this.fileInfo = fileInfo;

        using var stream = File.Open(this.fileInfo.FullName, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadBytes(3);
        var version = reader.ReadByte();
        if (!magic.SequenceEqual("SST"u8.ToArray()) || version != 3)
        {
            throw new FormatException("Not an SST file");
        }

        var sampleRate = reader.ReadUInt16();
        var count = (this.fileInfo.Length - 16 /* sizeof(header) */) / 4 /* sizeof(record) */;
        reader.ReadUInt16(); // padding
        var timestamp = reader.ReadInt64();

        var duration = TimeSpan.FromSeconds((double)count / sampleRate);
        ShouldBeImported = duration.TotalSeconds >= 5;
        StartTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        Duration = duration.ToString(@"hh\:mm\:ss");
        Name = fileInfo.Name;
        Description = $"Imported from {fileInfo.Name}";
    }
    
    public async Task<byte[]> GeneratePsstAsync(byte[] linkage, byte[] calibrations)
    {
        var rawData = await File.ReadAllBytesAsync(fileInfo.FullName);
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
    
    {
        var rawData = await File.ReadAllBytesAsync(fileInfo.FullName);
        var rawTelemetryData = new RawTelemetryData(rawData);
        var telemetryData = new TelemetryData(fileInfo.Name,
            rawTelemetryData.Version, rawTelemetryData.SampleRate, rawTelemetryData.Timestamp,
            frontCal, rearCal, linkage);
        return telemetryData.ProcessRecording(rawTelemetryData.Front, rawTelemetryData.Rear);
    }
    
    public void OnImported()
    {
        Imported = true;
        File.Move(fileInfo.FullName,
            $"{Path.GetDirectoryName(fileInfo.FullName)}/uploaded/{fileInfo.Name}");
    }
}