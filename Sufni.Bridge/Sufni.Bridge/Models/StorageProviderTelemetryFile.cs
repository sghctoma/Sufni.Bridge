using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Models;

public class StorageProviderTelemetryFile : ITelemetryFile
{
    private readonly IStorageFile storageFile;
    private Task Initialization { get; }

    public string Name { get; set; }
    public string FileName => storageFile.Name;
    public bool ShouldBeImported { get; set; }
    public bool Imported { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; private set; }
    public string Duration { get; private set; }

    private async Task Init()
    {
        await using var stream = await storageFile.OpenReadAsync();
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadBytes(3);
        var version = reader.ReadByte();
        if (!magic.SequenceEqual("SST"u8.ToArray()) || version != 3)
        {
            throw new FormatException("Not an SST file");
        }

        var sampleRate = reader.ReadUInt16();
        var count = (stream.Length - 16 /* sizeof(header) */) / 4 /* sizeof(record) */;
        reader.ReadUInt16(); // padding
        var timestamp = reader.ReadInt64();

        var duration = TimeSpan.FromSeconds((double)count / sampleRate);
        ShouldBeImported = duration.TotalSeconds >= 5;
        StartTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        Duration = duration.ToString(@"hh\:mm\:ss");
        Name = storageFile.Name;
        Description = $"Imported from {storageFile.Name}";
    }
    
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public StorageProviderTelemetryFile(IStorageFile storageFile)
    {
        this.storageFile = storageFile;
        Initialization = Init();
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    
    public async Task<byte[]> GeneratePsstAsync(Linkage linkage, Calibration? frontCal, Calibration? rearCal)
    {
        await Initialization;
        
        await using var stream = await storageFile.OpenReadAsync();
        var rawTelemetryData = new RawTelemetryData(stream);
        var telemetryData = new TelemetryData(storageFile.Name,
            rawTelemetryData.Version, rawTelemetryData.SampleRate, rawTelemetryData.Timestamp,
            frontCal, rearCal, linkage);
        return telemetryData.ProcessRecording(rawTelemetryData.Front, rawTelemetryData.Rear);
    }
    
    public async Task OnImported()
    {
        await Initialization;
        
        Imported = true;
        var parent = await storageFile.GetParentAsync();
        var parentItems = parent!.GetItemsAsync();
        IStorageFolder? uploaded = null;
        await foreach (var item in parentItems)
        {
            if (!item.Name.Equals("uploaded")) continue;
            uploaded = item as IStorageFolder;
            break;
        }

        if (uploaded is null)
        {
            throw new Exception("The \"uploaded\" folder could not be accessed.");
        }
        
        await storageFile.MoveAsync(uploaded);
    }
}