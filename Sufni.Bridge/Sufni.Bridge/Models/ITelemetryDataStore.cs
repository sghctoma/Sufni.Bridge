using System.Collections.Generic;

namespace Sufni.Bridge.Models;

public interface ITelemetryDataStore
{
    public string Name { get; }
    public IEnumerable<ITelemetryFile> Files { get; }
    public string BoardId { get; }
}