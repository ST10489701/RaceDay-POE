using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

/// <summary>Settings for Azure Blob Storage (section "BlobStorage" in appsettings.json).</summary>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>"UseDevelopmentStorage=true" uses the local Azurite emulator; use your Azure Storage account connection string in the cloud.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "raceday-images";
}

/// <summary>Thrown when the file storage cannot be reached or is not configured.</summary>
public class StorageUnavailableException : Exception
{
    public StorageUnavailableException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

/// <summary>A file read back from storage.</summary>
public record BlobDownload(Stream Content, string ContentType);

/// <summary>The only place in the whole system that talks to Azure Blob Storage.</summary>
public interface IBlobStorageService
{
    Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<BlobDownload?> DownloadAsync(string blobPath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default);
}

/// <summary>Stores files in a private Azure Blob container. The container is created on first use.</summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobStorageOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private BlobContainerClient? _container;

    public AzureBlobStorageService(IOptions<BlobStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = await GetContainerAsync(cancellationToken);
            var blob = container.GetBlobClient(blobPath);
            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is RequestFailedException or HttpRequestException or IOException or TaskCanceledException)
        {
            throw new StorageUnavailableException("Could not upload the file to storage.", ex);
        }
    }

    public async Task<BlobDownload?> DownloadAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = await GetContainerAsync(cancellationToken);
            var blob = container.GetBlobClient(blobPath);
            var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new BlobDownload(response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex) when (ex is RequestFailedException or HttpRequestException or IOException or TaskCanceledException)
        {
            throw new StorageUnavailableException("Could not read the file from storage.", ex);
        }
    }

    public async Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = await GetContainerAsync(cancellationToken);
            await container.GetBlobClient(blobPath).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is RequestFailedException or HttpRequestException or IOException or TaskCanceledException)
        {
            throw new StorageUnavailableException("Could not delete the file from storage.", ex);
        }
    }

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
    {
        if (_container is not null)
        {
            return _container;
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new StorageUnavailableException("Blob storage is not configured.");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_container is null)
            {
                var clientOptions = new BlobClientOptions();
                clientOptions.Retry.MaxRetries = 1;
                clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(15);

                var container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName, clientOptions);
                await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                _container = container;
            }

            return _container;
        }
        finally
        {
            _gate.Release();
        }
    }
}
