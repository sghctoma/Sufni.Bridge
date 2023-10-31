using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Sufni.Bridge.Services;

public interface IFilesService
{
    public Task<IStorageFile?> OpenLeverageRatioFileAsync();
}