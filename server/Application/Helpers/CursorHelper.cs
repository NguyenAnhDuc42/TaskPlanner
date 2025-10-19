using System;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Application.Helper;

public class CursorEncryptionOptions
{
    public const string SectionName = "CursorEncryption";
    public string Key { get; set; } = string.Empty;
}

public class CursorHelper
{
    private readonly byte[] _key;

    public CursorHelper(IOptions<CursorEncryptionOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.Key);
    }
    public string EncodeCursor(CursorData data)
    {
        var json = JsonSerializer.Serialize(data.Values);
        var encrypted = EncryptString(json, _key);
        return Convert.ToBase64String(encrypted);
    }

    public CursorData? DecodeCursor(string cursor)
    {
        try
        {
            var encrypted = Convert.FromBase64String(cursor);
            var json = DecryptString(encrypted, _key);
            var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return new CursorData(values);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] EncryptString(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return ms.ToArray();
    }

    private static string DecryptString(byte[] cipherText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;

        // read IV from start
        byte[] iv = new byte[aes.BlockSize / 8];
        Array.Copy(cipherText, iv, iv.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, iv);
        using var ms = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

}

public record CursorData(Dictionary<string, object> Values);