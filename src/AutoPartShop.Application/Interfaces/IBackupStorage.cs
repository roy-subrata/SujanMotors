namespace AutoPartShop.Application.Interfaces;

using AutoPartShop.Application.DTOs.BackupDtos;

/// <summary>
/// Cloud storage for database backup files (implemented by Google Drive in Infrastructure)
/// </summary>
public interface IBackupStorage
{
    /// <summary>
    /// Upload a local backup file into the configured cloud folder. Returns the cloud file id.
    /// </summary>
    Task<string> UploadAsync(string localFilePath, string fileName, string folderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a backup file from the cloud to the given local path.
    /// </summary>
    Task DownloadAsync(string cloudFileId, string destinationFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a backup file from the cloud. Must not throw when the file no longer exists.
    /// </summary>
    Task DeleteAsync(string cloudFileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that the credential is configured and the folder is accessible.
    /// </summary>
    Task<DriveStatusResult> ValidateAsync(string? folderId, CancellationToken cancellationToken = default);
}
