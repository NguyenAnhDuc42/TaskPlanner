using System;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;




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
        if (string.IsNullOrEmpty(options.Value.Key)) 
            throw new ArgumentException("Cursor encryption key is missing in configuration.");
        _key = Convert.FromBase64String(options.Value.Key);

        if (_key.Length != 16 && _key.Length != 24 && _key.Length != 32)
            throw new ArgumentException($"Invalid Key size ({_key.Length} bytes). AES key must be 16, 24, or 32 bytes.");
    }
    //EF CORE (legacy)
    // public string EncodeCursor(CursorData data)
    // {
    //     var options = new JsonSerializerOptions 
    //     { 
    //         Converters = { new JsonStringEnumConverter() }
    //     };
    //     var json = JsonSerializer.Serialize(data.Values, options);
    //     var encrypted = EncryptString(json, _key);
    //     return Convert.ToBase64String(encrypted);
    // }

    // public CursorData? DecodeCursor(string cursor)
    // {
    //     try
    //     {
    //         var encrypted = Convert.FromBase64String(cursor);
    //         var json = DecryptString(encrypted, _key);
    //         var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    //         return new CursorData(values!);
    //     }
    //     catch
    //     {
    //         return null;
    //     }
    // }
    public string EncodeCursor(CursorData data)
    {
        // Serialize with proper type handling
        var options = new JsonSerializerOptions 
        { 
            Converters = { new JsonStringEnumConverter() }
        };
        var json = JsonSerializer.Serialize(data.Values, options);
        var encrypted = EncryptString(json, _key);
        return Convert.ToBase64String(encrypted);
    }

    public CursorData? DecodeCursor(string cursor)
    {
        try
        {
            var encrypted = Convert.FromBase64String(cursor);
            var json = DecryptString(encrypted, _key);
            
            // Parse and convert properly
            using var doc = JsonDocument.Parse(json);
            var values = new Dictionary<string, object>();
            
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                object value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number when prop.Value.TryGetInt64(out var l) => l,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.ToString()
                };
                
                values[prop.Name] = value;
            }
            
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
        
        // Use a fixed IV for deterministic encryption (same data = same cursor)
        aes.IV = new byte[aes.BlockSize / 8]; 

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV (still good practice)

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