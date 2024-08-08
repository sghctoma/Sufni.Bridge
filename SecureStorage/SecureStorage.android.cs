using System.Diagnostics;
using Android.Content;
using AndroidX.Security.Crypto;
using Xamarin.Google.Crypto.Tink.Shaded.Protobuf;

namespace SecureStorage;

public class SecureStorage : ISecureStorage
{
    private const string Alias = "sufni.bridge.android.preferences";
    private ISharedPreferences? sharedPreferences;

    public SecureStorage()
    {
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        sharedPreferences = await GetEncryptedSharedPreferences();
    }

    private async Task<ISharedPreferences?> GetEncryptedSharedPreferences()
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
            await RemoveAllAsync();
            return null;
        }
    }

    public Task<byte[]?> GetAsync(string key)
    {
        var value = sharedPreferences?.GetString(key, null);
        return Task.FromResult(value is null ? null : Convert.FromBase64String(value));
    }

    public Task<string?> GetStringAsync(string key)
    {
        return Task.FromResult(sharedPreferences?.GetString(key, null));
    }

    public Task SetAsync(string key, byte[]? value)
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
        return Task.CompletedTask;
    }

    public Task SetStringAsync(string key, string? value)
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
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        using var editor = sharedPreferences?.Edit();
        editor?.Remove(key);
        editor?.Apply();
        return Task.CompletedTask;
    }

    public Task RemoveAllAsync()
    {
        using var editor = sharedPreferences?.Edit();
        editor?.Clear();
        editor?.Apply();
        return Task.CompletedTask;
    }
}