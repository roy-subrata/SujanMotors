using AutoPartShop.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AutoPartShop.Infrastructure.Services.Storage;

/// <summary>
/// Stores uploaded blobs on the local filesystem under "FileStorage:Local:RootPath"
/// (default: {ContentRoot}/App_Data/uploads — on the VPS, bind-mount this path so
/// files survive container rebuilds). Keys are generated server-side, but paths are
/// still validated against the root to rule out traversal.
/// </summary>
public class LocalDiskFileStorage : IFileStorageService
{
    private readonly string _rootPath;

    public LocalDiskFileStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["FileStorage:Local:RootPath"];
        _rootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "uploads")
            : configured);
    }

    public async Task SaveAsync(Stream content, string storageKey, string contentType, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Write to a temp file first so a failed/aborted upload never leaves a
        // half-written blob at the final key.
        var tempPath = fullPath + ".tmp";
        try
        {
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key cannot be empty", nameof(storageKey));

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Storage key '{storageKey}' resolves outside the storage root.");

        return fullPath;
    }
}
