using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StorageProvider
{
    Unknown,
    Local,
    S3,
    AzureBlob,
    GoogleCloudStorage,
}

