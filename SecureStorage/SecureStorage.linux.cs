using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SecureStorageDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>;

namespace SecureStorage;

public class SecureStorage : ISecureStorage
{
    private static readonly string AppSecureStoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "Sufni.Bridge",
        "preferences.dat");

    private readonly SecureStorageDictionary secureStorage = new();

    public SecureStorage()
    {
        Load();
    }
    
    private void Load()
    {
        if (!File.Exists(AppSecureStoragePath))
            return;

        try
        {
            using var stream = File.OpenRead(AppSecureStoragePath);
            var readPreferences = JsonSerializer.Deserialize<SecureStorageDictionary>(stream);

            if (readPreferences != null)
            {
                secureStorage.Clear();
                foreach (var pair in readPreferences)
                    secureStorage.TryAdd(pair.Key, pair.Value);
            }
        }
        catch (JsonException)
        {
            // if deserialization fails proceed with empty settings
        }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(AppSecureStoragePath);
        Debug.Assert(dir != null, nameof(dir) + " != null");
        Directory.CreateDirectory(dir);

        using var stream = File.Create(AppSecureStoragePath);
        JsonSerializer.Serialize(stream, secureStorage);
    }
    
    public byte[]? Get(string key)
    {
        if (!secureStorage.TryGetValue(key, out var value))
        {
            return null;
        }

        // TODO: unprotect
        return value;
    }

    public string? GetString(string key)
    {
        if (!secureStorage.TryGetValue(key, out var value))
        {
            return null;
        }

        // TODO: unprotect
        return Encoding.UTF8.GetString(value);
    }

    public void Set(string key, byte[]? value)
    {
        if (value is null)
        {
            secureStorage.TryRemove(key, out _);
        }
        else
        {
            // TODO: protect
            secureStorage[key] = value;
        }

        Save();
    }

    public void SetString(string key, string? value)
    {
        if (value is null)
        {
            secureStorage.TryRemove(key, out _);
        }
        else
        {
            var data = Encoding.UTF8.GetBytes(s: value);
            // TODO: protect
            secureStorage[key] = data;
        }

        Save();
    }

    public void Remove(string key)
    {
        secureStorage.Remove(key, out _);
        Save();
    }

    public void RemoveAll()
    {
        secureStorage.Clear();
        Save();
    }
}