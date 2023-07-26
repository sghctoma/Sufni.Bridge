namespace SecureStorage;

public interface ISecureStorage
{
    byte[]? Get(string key);

    string? GetString(string key);

    void Set(string key, byte[]? value);

    void SetString(string key, string value);

    bool Remove(string key);

    void RemoveAll();
}