using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace src.Helper;

public class CursorData
{
    public Guid Id { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? StringValue { get; set; }
    public int? IntValue { get; set; }
}

public static class CursorHelper
{
    public static string EncodeCursor(CursorData data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
    }

    public static CursorData? DecodeCursor(string cursor)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return System.Text.Json.JsonSerializer.Deserialize<CursorData>(json);
        }
        catch
        {
            return null;
        }
    }
}