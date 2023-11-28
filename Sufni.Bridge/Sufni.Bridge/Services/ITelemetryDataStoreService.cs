using Sufni.Bridge.Models;
using System.Collections.ObjectModel;

namespace Sufni.Bridge.Services;

public interface ITelemetryDataStoreService
{
    public ObservableCollection<ITelemetryDataStore> DataStores { get; }
}
