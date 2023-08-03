using System;
using System.IO;
using System.Linq;

namespace Sufni.Bridge.Models;

public class TelemetryFile
{
    public string FileName => _fileInfo.Name;
    public string FullName => _fileInfo.FullName;
    public DateTime StartTime { get; }
    public string Name { get; set; } = "";
    public string Duration => _duration.ToString("hh\\:mm\\:ss");
    public bool ShouldBeImported { get; set; }
    public bool Imported { get; set; }
    public string Data => Convert.ToBase64String(File.ReadAllBytes(FullName));

    public string Description { get; set; } = "";

    private readonly FileInfo _fileInfo;
    private readonly TimeSpan _duration;

    public TelemetryFile(FileInfo fileInfo)
    {
        _fileInfo = fileInfo;

        using var stream = File.Open(_fileInfo.FullName, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadBytes(3);
        var version = reader.ReadByte();
        if (!magic.SequenceEqual("SST"u8.ToArray()) || version != 3)
        {
            throw new FormatException("Not an SST file");
        }

        var sampleRate = reader.ReadUInt16();
        var count = (_fileInfo.Length - 16 /* sizeof(header) */) / 4 /* sizeof(record) */;
        _duration = TimeSpan.FromSeconds((double)count / sampleRate);
        ShouldBeImported = _duration.TotalSeconds >= 5;

        reader.ReadUInt16(); // padding

        StartTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64()).DateTime;
    }
}