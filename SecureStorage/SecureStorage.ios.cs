using System.Text;
using Security;

namespace SecureStorage;

public class SecureStorage : ISecureStorage
{
    private const string Alias = "sufni.bridge.ios.preferences";
    private const SecAccessible Accessible = SecAccessible.WhenUnlocked;

    private static SecRecord RecordForKey(string key)
    {
	    return new SecRecord(SecKind.GenericPassword)
	    {
		    Account = key,
		    Service = Alias
	    };
    }

    private static SecRecord CreateRecord(string key, byte[] value)
    {
	    return new SecRecord(SecKind.GenericPassword)
	    {
		    Account = key,
		    Service = Alias,
		    Label = key,
		    Accessible = Accessible,
		    ValueData = NSData.FromArray(value),
	    };
    }

    public byte[]? Get(string key)
    {
	    using var record = RecordForKey(key);
	    using var match = SecKeyChain.QueryAsRecord(record, out var result);
	    return result == SecStatusCode.Success ? match!.ValueData!.ToArray() : null;
    }

    public string? GetString(string key)
    {
	    using var record = RecordForKey(key);
	    using var match = SecKeyChain.QueryAsRecord(record, out var result);
	    return result == SecStatusCode.Success ? NSString.FromData(match!.ValueData!, NSStringEncoding.UTF8) : null;
    }

    public void Set(string key, byte[]? value)
    {
	    Remove(key);

	    if (value is null)
	    {
		    return;
	    }

	    using var record = CreateRecord(key, value);
	    var result = SecKeyChain.Add(record);
	    if (result != SecStatusCode.Success)
	    {
		    throw new Exception($"Error adding record: {result}");
	    }
    }

    public void SetString(string key, string? value)
    {
	    var bytes = value is not null ? Encoding.UTF8.GetBytes(s: value) : null;
	    Set(key, bytes);
    }

    public void Remove(string key)
    {
	    using var record = RecordForKey(key);
	    var result = SecKeyChain.Remove(record);
	    if (result != SecStatusCode.Success && result != SecStatusCode.ItemNotFound)
		    throw new Exception($"Error removing record: {result}");
    }

    public void RemoveAll()
    {
	    using var query = new SecRecord(SecKind.GenericPassword);
	    query.Service = Alias;
	    SecKeyChain.Remove(query);
    }
}