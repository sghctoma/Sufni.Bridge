using Sufni.Bridge.Models;
using System.Collections.Generic;

namespace Sufni.Bridge.Services;

public interface ITelemetryDataStoreService
{
    public IEnumerable<TelemetryDataStore> GetTelemetryDataStores();
}
