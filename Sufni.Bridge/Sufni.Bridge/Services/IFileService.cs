using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Sufni.Bridge.Services;

public interface IFilesService
{
    public void SetTarget(TopLevel? target);
    public Task<IStorageFile?> OpenLeverageRatioFileAsync();
    public Task<IStorageFolder?> OpenDataStoreFolderAsync();
}