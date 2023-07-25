using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Sufni.Bridge.Models;

public class TelemetrySession
{
    public string FileName => _fileInfo.Name;
    public string FullName => _fileInfo.FullName;
    public DateTime StartTime { get; }
    public string Name { get; set; } = "";
    public string Duration => _duration.ToString("hh\\:mm\\:ss");
    public bool ShouldBeImported { get; set; }

    public string Description { get; set; } = "";

    private readonly FileInfo _fileInfo;
    private readonly TimeSpan _duration;
    private readonly string _boardId;

    public TelemetrySession(FileInfo fileInfo, string boardId)
    {
        _fileInfo = fileInfo;
        _boardId = boardId;

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

    public string ToJson()
    {
        var session = new
        {
            name = Name,
            description = Description,
            board = _boardId,
            data = Convert.ToBase64String(File.ReadAllBytes(FullName))
        };

        return JsonSerializer.Serialize<object>(session);
    }
}