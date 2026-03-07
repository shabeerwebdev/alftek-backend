using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Common.Interfaces;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// File storage backed by Cloudflare R2 (S3-compatible API).
/// Endpoint: https://{accountId}.r2.cloudflarestorage.com
/// </summary>
public class CloudflareR2StorageService : IFileStorageService, IDisposable
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly ILogger<CloudflareR2StorageService> _logger;

    public CloudflareR2StorageService(IConfiguration config, ILogger<CloudflareR2StorageService> logger)
    {
        _logger = logger;
        _bucket = config["Storage:R2BucketName"] ?? "alftekpro-hrms";

        var accountId  = config["Storage:R2AccountId"] ?? string.Empty;
        var accessKey  = config["Storage:R2AccessKeyId"] ?? string.Empty;
        var secretKey  = config["Storage:R2SecretAccessKey"] ?? string.Empty;
        var endpoint   = config["Storage:R2Endpoint"]
                         ?? $"https://{accountId}.r2.cloudflarestorage.com";

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var s3Config = new AmazonS3Config
        {
            ServiceURL            = endpoint,
            ForcePathStyle        = true,   // required for R2
            SignatureVersion      = "4",
            RegionEndpoint        = RegionEndpoint.USEast1 // placeholder; R2 ignores region
        };

        _s3 = new AmazonS3Client(credentials, s3Config);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName  = _bucket,
            Key         = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await _s3.PutObjectAsync(request, ct);
        _logger.LogInformation("Uploaded file {Key} to R2 bucket {Bucket}", key, _bucket);
        return key;
    }

    public Task<string> GetSignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key        = key,
            Expires    = DateTime.UtcNow.Add(expiry),
            Verb       = HttpVerb.GET
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(_bucket, key, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _s3.DeleteObjectAsync(_bucket, key, ct);
        _logger.LogInformation("Deleted file {Key} from R2 bucket {Bucket}", key, _bucket);
    }

    public void Dispose() => _s3.Dispose();
}
