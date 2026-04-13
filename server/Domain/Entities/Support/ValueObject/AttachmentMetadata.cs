using Domain.Enums;
using System.Text.Json;

namespace Domain.Entities;


public record AttachmentMetadata
{
    public static AttachmentMetadata Create(AttachmentType type, string json) => type switch
    {
        AttachmentType.Embed => EmbedMetadata.FromJson(json),
        AttachmentType.Link => LinkMetadata.FromJson(json),
        AttachmentType.Media => MediaMetaData.FromJson(json),
        AttachmentType.File => FileMetadata.FromJson(json),
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type {type} not supported")
    };
};



public record EmbedMetadata(string EmbedUrl, string Provider) : AttachmentMetadata
{
    public static EmbedMetadata FromJson(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new EmbedMetadata(
            root.GetProperty("embedUrl").GetString() ?? string.Empty,
            root.GetProperty("provider").GetString() ?? string.Empty
        );
    }
    public string ToJson() => JsonSerializer.Serialize(this);
}
public record FileMetadata(
    string? Extension,
    string? PageCount = null,      // For PDFs
    bool? IsPasswordProtected = false
) : AttachmentMetadata
{
    public static FileMetadata FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new FileMetadata(
            Extension: root.TryGetProperty("extension", out var e) ? e.GetString() : null,
            PageCount: root.TryGetProperty("pageCount", out var p) ? p.GetString() : null,
            IsPasswordProtected: root.TryGetProperty("isPasswordProtected", out var pw) && pw.GetBoolean()
        );
    }

    public string ToJson() => JsonSerializer.Serialize(this);
}
public record LinkMetadata(
    string Url,
    string? Title = null,
    string? Description = null,
    string? ImageUrl = null,
    string? FaviconUrl = null,
    string? SiteName = null
) : AttachmentMetadata
{
    public static LinkMetadata FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new LinkMetadata(
            Url: root.GetProperty("url").GetString() ?? throw new InvalidOperationException("URL required"),
            Title: root.TryGetProperty("title", out var t) ? t.GetString() : null,
            Description: root.TryGetProperty("description", out var d) ? d.GetString() : null,
            ImageUrl: root.TryGetProperty("imageUrl", out var i) ? i.GetString() : null,
            FaviconUrl: root.TryGetProperty("faviconUrl", out var f) ? f.GetString() : null,
            SiteName: root.TryGetProperty("siteName", out var s) ? s.GetString() : null
        );
    }

    public string ToJson() => JsonSerializer.Serialize(this);
}
public record MediaMetaData(int? Width,int? Height ,int? DurationSecconds) : AttachmentMetadata
{
    public static MediaMetaData FromJson(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new MediaMetaData(
            root.TryGetProperty("width", out var w) ? w.GetInt32() : null,
            root.TryGetProperty("height", out var h) ? h.GetInt32() : null,
            root.TryGetProperty("durationSecconds", out var d) ? d.GetInt32() : null
        );
    }

    public string ToJson() => JsonSerializer.Serialize(this );
}
