using System.Text.Json.Serialization;

namespace Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StorageProvider
{
    Unknown,
    Local,
    S3,
    AzureBlob,
    GoogleCloudStorage,
}
