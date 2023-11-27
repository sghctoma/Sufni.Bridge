using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sufni.Bridge.Models;

public class MassStorageTelemetryDataStore : ITelemetryDataStore
{
    public string Name { get; set; }
    public IEnumerable<ITelemetryFile> Files { get; }
    public string BoardId { get; }

    public MassStorageTelemetryDataStore(string name, DirectoryInfo path)
    {
        Name = name;
        BoardId = File.ReadAllText($"{path.FullName}/.boardid");
        Files = path.GetFiles("*.SST")
            .Select(f => new MassStorageTelemetryFile(f))
            .OrderBy(f => f.StartTime)
            .ToList();

        if (!Directory.Exists($"{path.FullName}/uploaded"))
            Directory.CreateDirectory($"{path.FullName}/uploaded");
    }
}