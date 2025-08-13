using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace src.Helper;

/// <summary>
/// Small, generic cursor helper producing Base64 JSON cursors with { value, id }.
/// Designed to be reusable across handlers/entities.
/// </summary>
public static class CursorHelper
{
    private sealed class Payload
    {
        public string? Value { get; set; }
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Generate a Base64 JSON cursor for an item.
    /// Provide a selector that yields the sort value and an Id selector for the tie-breaker.
    /// The value is serialized to a string; DateTime uses round-trip ("o") format.
    /// </summary>
    public static string GenerateCursor<T>(T item, Func<T, object?> valueSelector, Func<T, Guid> idSelector)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        var rawValue = valueSelector(item);

        string? valueString = rawValue switch
        {
            null => null,
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => rawValue.ToString()
        };

        var payload = new Payload
        {
            Value = valueString,
            Id = idSelector(item)
        };

        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Decode a cursor into its raw string value and Guid id.
    /// Does not perform type conversion — leave conversion to the caller (so helper remains generic).
    /// </summary>
    public static (string? Value, Guid Id) DecodeCursor(string cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) throw new ArgumentException("Cursor empty.", nameof(cursor));

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(cursor);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Cursor is not a valid Base64 string.", ex);
        }

        var json = Encoding.UTF8.GetString(bytes);

        Payload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Cursor payload is not valid JSON.", ex);
        }

        if (payload == null || payload.Id == Guid.Empty)
            throw new ArgumentException("Cursor payload missing required data.");

        return (payload.Value, payload.Id);
    }

    // --- Conversion helpers for common sort value types ---
    public static DateTime? ParseDateTimeOrNull(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.ParseExact(s!, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public static DateTime ParseDateTime(string s) =>
        DateTime.ParseExact(s, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static int ParseInt(string s) => int.Parse(s, CultureInfo.InvariantCulture);

    public static int? ParseIntOrNull(string? s) =>
        string.IsNullOrEmpty(s) ? (int?)null : int.Parse(s!, CultureInfo.InvariantCulture);

    public static string? ParseStringOrNull(string? s) => s;
}

