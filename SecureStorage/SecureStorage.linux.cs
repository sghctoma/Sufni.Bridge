using System.Diagnostics;
using System.Text;
using DBus.Services.Secrets;

namespace SecureStorage;

public class SecureStorage : ISecureStorage
{
    private const string ContentTypeText = "text/plain; charset=utf8";
    private const string ContentTypeBytes = "application/octet-stream";
    private const string LabelPrefix = "Sufni Suspension Telemetry";

    private Collection? defaultCollection;

    public SecureStorage()
    {
        var secretService = Task.Run(async () => await SecretService.ConnectAsync(EncryptionType.Dh)).Result;
        defaultCollection = Task.Run(async () => await secretService.GetDefaultCollectionAsync()).Result;
        
        if (defaultCollection is null)
        {
            throw new Exception("Could not access secret storage.");
        }
    }

    private static Dictionary<string, string> GetAttributes(string? key)
    {
        var attributes = new Dictionary<string, string>
        {
            { "service", "sufni-suspension-telemetry" },
        };
        if (key is not null)
        {
            attributes.Add("sufni-suspension-telemetry-api-setting", key);
        }

        return attributes;
    }
    
    private void CreateItem(string key, object? value)
    {
        Debug.Assert(defaultCollection != null, nameof(defaultCollection) + " != null");
        
        if (value is null)
        {
            Remove(key);
        }

        if (value is not (byte[] or string))
        {
            throw new Exception("Invalid value type!");
        }
        
        var createdItem = Task.Run(async () => await defaultCollection.CreateItemAsync(
            $"{LabelPrefix} ({key})",
            GetAttributes(key),
            value as byte[] ?? Encoding.UTF8.GetBytes((value as string)!),
            value is byte[] ? ContentTypeBytes : ContentTypeText,
            true)).Result;

        if (createdItem == null)
        {
            throw new Exception($"Could not save {key}");
        }
    }

    private void DeleteItems(string? key)
    {
        Debug.Assert(defaultCollection != null, nameof(defaultCollection) + " != null");
        
        var attributes = GetAttributes(key);
        var matchedItems = Task.Run(async () => await defaultCollection.SearchItemsAsync(attributes)).Result;
        foreach (var matchedItem in matchedItems)
        {
            Task.Run(async () => await matchedItem.DeleteAsync()).Wait();
        }
    }

    private byte[]? SearchItem(string key)
    {
        Debug.Assert(defaultCollection != null, nameof(defaultCollection) + " != null");
      
        var attributes = GetAttributes(key);
        var matchedItems = Task.Run(async () => await defaultCollection.SearchItemsAsync(attributes)).Result;
        byte[]? secret = null;
        if (matchedItems.Length == 1)
        {
            secret = Task.Run(async () => await matchedItems[0].GetSecretAsync()).Result;
        }
        
        if (matchedItems.Length > 1)
        {
            throw new Exception("Duplicate items!");
        }

        return secret;
    }
    
    public byte[]? Get(string key)
    {
        return SearchItem(key);
    }

    public string? GetString(string key)
    {
        var valueBytes = SearchItem(key);
        return valueBytes is not null ? Encoding.UTF8.GetString(valueBytes) : null;
    }

    public void Set(string key, byte[]? value)
    {
        CreateItem(key, value);
    }

    public void SetString(string key, string? value)
    {
        CreateItem(key, value);
    }

    public void Remove(string key)
    {
        DeleteItems(key);
    }

    public void RemoveAll()
    {
        DeleteItems(null);
    }
}