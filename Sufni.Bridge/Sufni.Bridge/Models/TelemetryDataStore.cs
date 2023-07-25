using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sufni.Bridge.Models;

public class TelemetryDataStore
{
    public string Name { get; set; }
    public DirectoryInfo Path { get; set; }
    public List<TelemetrySession> Files { get; set; }
    public string BoardId { get; }

    public TelemetryDataStore(string name, DirectoryInfo path)
    {
        Name = name;
        Path = path;

        BoardId = File.ReadAllText($"{Path.FullName}/.boardid");

        Files = Path.GetFiles("*.SST")
            .Select(f => new TelemetrySession(f, BoardId))
            .OrderBy(f => f.StartTime)
            .ToList();
    }
}