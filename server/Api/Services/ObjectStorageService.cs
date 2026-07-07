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

    public async Task<string> UploadAsync(Stream content, string key, string contentType, string? downloadFileName = null, CancellationToken cancellationToken = default)
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

        // Media (image/video) is rendered inline by the block editor via <img>/<video src> — it
        // must NOT be forced to download. Everything else (the file block's "Download" button
        // just does window.open(url)) needs Content-Disposition: attachment or the browser
        // renders it in-tab instead of saving it, for any type the browser knows how to display
        // (PDF, text, etc).
        if (downloadFileName != null)
            request.Headers.ContentDisposition = $"attachment; filename=\"{downloadFileName}\"";

        await client.PutObjectAsync(request, cancellationToken);

        return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{key}";
    }
}
