namespace SecureStorage;

public interface ISecureStorage
{
    byte[]? Get(string key);

    void Set(string key, byte[]? value);

    bool Remove(string key);

    void RemoveAll();
}