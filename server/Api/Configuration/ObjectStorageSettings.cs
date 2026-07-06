namespace Api;

public class ObjectStorageSettings
{
    public const string SectionName = "ObjectStorage";

    // Full S3-compatible endpoint (e.g. https://{accountId}.r2.cloudflarestorage.com for
    // Cloudflare R2). Generic naming so a future switch to another S3-compatible provider
    // doesn't require a rename.
    public string ServiceUrl { get; set; } = "";
    public string BucketName { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";

    // Public base URL files are served from (a custom domain, or the bucket's public r2.dev URL).
    public string PublicBaseUrl { get; set; } = "";
}
