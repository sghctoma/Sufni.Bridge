using Sufni.Bridge.Models;
using System.Collections.Generic;

namespace Sufni.Bridge.Services;

public interface ITelemetryFileService
{
    public IEnumerable<TelemetryFile> GetTelemetryFiles();
}
