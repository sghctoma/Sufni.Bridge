using System.Threading.Tasks;

namespace Sufni.Bridge.Services;

public interface ISynchronizationService
{
    public Task SyncAll();
}
