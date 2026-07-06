using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Api;

public class ObjectStorageService
{
    private readonly ObjectStorageSettings _settings;
    private AmazonS3Client? _client;

    public ObjectStorageService(IOptions<ObjectStorageSettings> options)
    {
        _settings = options.Value;
    }

    private AmazonS3Client GetClient()
    {
        if (string.IsNullOrWhiteSpace(_settings.ServiceUrl) || string.IsNullOrWhiteSpace(_settings.BucketName))
            throw new InvalidOperationException("Object storage is not configured (ObjectStorage:ServiceUrl / ObjectStorage:BucketName).");

        return _client ??= new AmazonS3Client(_settings.AccessKeyId, _settings.SecretAccessKey, new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            ForcePathStyle = true,
            // R2 doesn't implement the SDK's newer "flexible checksums" trailer format.
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
        });
    }

    public async Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default)
    {
        var client = GetClient();

        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            UseChunkEncoding = false,
        };

        await client.PutObjectAsync(request, cancellationToken);

        return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{key}";
    }
}
