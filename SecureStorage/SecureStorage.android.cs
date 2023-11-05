using System.Diagnostics;
using Android.Content;
using AndroidX.Security.Crypto;
using Xamarin.Google.Crypto.Tink.Shaded.Protobuf;

namespace SecureStorage;

public class SecureStorage : ISecureStorage
{
    private const string Alias = "sufni.bridge.android.preferences";
    private readonly ISharedPreferences? sharedPreferences;

    public SecureStorage()
    {
        sharedPreferences = GetEncryptedSharedPreferences();
    }
    
    private ISharedPreferences? GetEncryptedSharedPreferences()
    {
        Debug.Assert(EncryptedSharedPreferences.PrefValueEncryptionScheme.Aes256Gcm != null, 
            "EncryptedSharedPreferences.PrefValueEncryptionScheme.Aes256Gcm != null");
        Debug.Assert(EncryptedSharedPreferences.PrefKeyEncryptionScheme.Aes256Siv != null, 
            "EncryptedSharedPreferences.PrefKeyEncryptionScheme.Aes256Siv != null");
        
        try
        {
            var context = Application.Context;
            var prefsMainKey = new MasterKey.Builder(context, Alias)
                .SetKeyScheme(MasterKey.KeyScheme.Aes256Gcm!)
                .Build();
            var pref = EncryptedSharedPreferences.Create(
                context,
                Alias,
                prefsMainKey,
                EncryptedSharedPreferences.PrefKeyEncryptionScheme.Aes256Siv,
                EncryptedSharedPreferences.PrefValueEncryptionScheme.Aes256Gcm);

            return pref;
        }
        catch (InvalidProtocolBufferException)
        {
            RemoveAll();
            return null;
        }
    }

    public byte[]? Get(string key)
    {
        var value = sharedPreferences?.GetString(key, null);
        return value is null ? null : Convert.FromBase64String(value);
    }

    public string? GetString(string key)
    {
        return sharedPreferences?.GetString(key, null);
    }

    public void Set(string key, byte[]? value)
    {
        using var editor = sharedPreferences?.Edit();
        if (value is null)
        {
            editor?.Remove(key);
        }
        else
        {
            editor?.PutString(key, Convert.ToBase64String(value));
        }

        editor?.Apply();
    }

    public void SetString(string key, string? value)
    {
        using var editor = sharedPreferences?.Edit();
        if (value is null)
        {
            editor?.Remove(key);
        }
        else
        {
            editor?.PutString(key, value);
        }

        editor?.Apply();
    }

    public void Remove(string key)
    {
        using var editor = sharedPreferences?.Edit();
        editor?.Remove(key);
        editor?.Apply();
    }

    public void RemoveAll()
    {
        using var editor = sharedPreferences?.Edit();
        editor?.Clear();
        editor?.Apply();
    }
}