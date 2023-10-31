using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Sufni.Bridge.Services;

public class FilesService : IFilesService
{
    private readonly TopLevel target;

    public FilesService(TopLevel target)
    {
        this.target = target;
    }

    public async Task<IStorageFile?> OpenLeverageRatioFileAsync()
    {
        var files = await target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open Leverage Ratio file",
            AllowMultiple = false
        });

        return files.Count == 1 ? files[0] : null;
    }
}