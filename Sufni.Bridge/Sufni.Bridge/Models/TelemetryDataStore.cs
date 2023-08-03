using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sufni.Bridge.Models;

public class TelemetryDataStore
{
    public string Name { get; set; }
    public DirectoryInfo Path { get; set; }
    public List<TelemetryFile> Files { get; set; }
    public string BoardId { get; }

    public TelemetryDataStore(string name, DirectoryInfo path)
    {
        Name = name;
        Path = path;

        BoardId = File.ReadAllText($"{Path.FullName}/.boardid");

        Files = Path.GetFiles("*.SST")
            .Select(f => new TelemetryFile(f))
            .OrderBy(f => f.StartTime)
            .ToList();

        if (!Directory.Exists($"{Path.FullName}/uploaded"))
            Directory.CreateDirectory($"{Path.FullName}/uploaded");
    }
}