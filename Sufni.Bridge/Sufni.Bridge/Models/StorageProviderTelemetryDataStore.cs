using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Sufni.Bridge.Models;

public class StorageProviderTelemetryDataStore : ITelemetryDataStore
{
    private Task Initialization { get; }
    public string Name { get; }
    public string? BoardId { get; private set; }
    private IStorageFolder Folder { get; }

    public async Task<List<ITelemetryFile>> GetFiles()
    {
        await Initialization;

        var files = new List<ITelemetryFile>();
        var items = Folder.GetItemsAsync();
        await foreach (var item in items) 
        {
            if (item.Name.EndsWith(".SST") && item is IStorageFile file)
            {
                files.Add(new StorageProviderTelemetryFile(file));
            }
        }

        return files.OrderByDescending(f => f.StartTime).ToList();
    }

    private async Task Init()
    {
        await Folder.CreateFolderAsync("uploaded");

        IStorageFile? boardIdFile = null;
        var items = Folder.GetItemsAsync();
        await foreach (var item in items)
        {
            if (!item.Name.Equals(".boardid") || item is not IStorageFile file) continue;
            boardIdFile = file;
            break;
        }

        if (boardIdFile is null) return;

        await using var stream = await boardIdFile.OpenReadAsync();
        var buffer = new byte[16];
        await stream.ReadExactlyAsync(buffer, 0, 16);
        BoardId = Encoding.ASCII.GetString(buffer).ToLower();
    }
    
    public StorageProviderTelemetryDataStore(IStorageFolder folder)
    {
        Folder = folder;
        Name = folder.Name;
        Initialization = Init();
    }
}