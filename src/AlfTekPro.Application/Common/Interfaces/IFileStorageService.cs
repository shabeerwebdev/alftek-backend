namespace AlfTekPro.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads content to the object store and returns the object key.
    /// </summary>
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Generates a pre-signed URL valid for the given duration.
    /// </summary>
    Task<string> GetSignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>
    /// Returns a readable stream for the object at <paramref name="key"/>.
    /// </summary>
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Deletes the object at <paramref name="key"/>.
    /// </summary>
    Task DeleteAsync(string key, CancellationToken ct = default);
}
