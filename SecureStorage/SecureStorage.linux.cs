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
    
    readonly SecureStorageDictionary _secureStorage = new();

    void Load()
    {
        if (!File.Exists(AppSecureStoragePath))
            return;

        try
        {
            using var stream = File.OpenRead(AppSecureStoragePath);
            var readPreferences = JsonSerializer.Deserialize<SecureStorageDictionary>(stream);

            if (readPreferences != null)
            {
                _secureStorage.Clear();
                foreach (var pair in readPreferences)
                    _secureStorage.TryAdd(pair.Key, pair.Value);
            }
        }
        catch (JsonException)
        {
            // if deserialization fails proceed with empty settings
        }
    }

    void Save()
    {
        var dir = Path.GetDirectoryName(AppSecureStoragePath);
        Debug.Assert(dir != null, nameof(dir) + " != null");
        Directory.CreateDirectory(dir);

        using var stream = File.Create(AppSecureStoragePath);
        JsonSerializer.Serialize(stream, _secureStorage);
    }
    
    public byte[]? Get(string key)
    {
        if (!_secureStorage.TryGetValue(key, out var value))
        {
            return null;
        }

        // TODO: unprotect
        return value;
    }

    public string? GetString(string key)
    {
        if (!_secureStorage.TryGetValue(key, out var value))
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
            _secureStorage.TryRemove(key, out _);
        }
        else
        {
            // TODO: protect
            _secureStorage[key] = value;
        }

        Save();
    }

    public void SetString(string key, string? value)
    {
        if (value is null)
        {
            _secureStorage.TryRemove(key, out _);
        }
        else
        {
            var data = Encoding.UTF8.GetBytes(s: value);
            // TODO: protect
            _secureStorage[key] = data;
        }

        Save();
    }

    public bool Remove(string key)
    {
        var result = _secureStorage.TryRemove(key, out _);
        Save();
        return result;
    }

    public void RemoveAll()
    {
        _secureStorage.Clear();
        Save();
    }
}