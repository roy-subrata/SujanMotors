namespace AutoPartShop.Infrastructure.Services.Backup;

using System.Text.Json;
using AutoPartShop.Application.DTOs.BackupDtos;
using AutoPartShop.Application.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Google Drive implementation of <see cref="IBackupStorage"/> using a service account.
/// The admin shares a Drive folder (or Shared Drive) with the service account email as Editor
/// and configures the folder id in application settings.
/// </summary>
public sealed class GoogleDriveBackupStorage(
    IConfiguration configuration,
    ILogger<GoogleDriveBackupStorage> logger) : IBackupStorage
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<GoogleDriveBackupStorage> _logger = logger;
    private readonly object _serviceLock = new();
    private DriveService? _driveService;

    public async Task<string> UploadAsync(
        string localFilePath,
        string fileName,
        string folderId,
        CancellationToken cancellationToken = default)
    {
        var service = GetDriveService();

        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = [folderId]
        };

        await using var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
        var request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
        request.SupportsAllDrives = true;
        request.Fields = "id";

        var progress = await request.UploadAsync(cancellationToken);
        if (progress.Status != UploadStatus.Completed)
            throw new InvalidOperationException(
                $"Google Drive upload of '{fileName}' failed: {progress.Exception?.Message ?? progress.Status.ToString()}",
                progress.Exception);

        _logger.LogInformation("Uploaded backup {FileName} to Google Drive (file id {FileId})",
            fileName, request.ResponseBody.Id);
        return request.ResponseBody.Id;
    }

    public async Task DownloadAsync(
        string cloudFileId,
        string destinationFilePath,
        CancellationToken cancellationToken = default)
    {
        var service = GetDriveService();

        var request = service.Files.Get(cloudFileId);
        request.SupportsAllDrives = true;

        await using var stream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);
        var progress = await request.DownloadAsync(stream, cancellationToken);
        if (progress.Status != Google.Apis.Download.DownloadStatus.Completed)
            throw new InvalidOperationException(
                $"Google Drive download of file {cloudFileId} failed: {progress.Exception?.Message ?? progress.Status.ToString()}",
                progress.Exception);

        _logger.LogInformation("Downloaded backup from Google Drive (file id {FileId}) to {Path}",
            cloudFileId, destinationFilePath);
    }

    public async Task DeleteAsync(string cloudFileId, CancellationToken cancellationToken = default)
    {
        var service = GetDriveService();

        try
        {
            var request = service.Files.Delete(cloudFileId);
            request.SupportsAllDrives = true;
            await request.ExecuteAsync(cancellationToken);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Google Drive file {FileId} already deleted", cloudFileId);
        }
    }

    public async Task<DriveStatusResult> ValidateAsync(string? folderId, CancellationToken cancellationToken = default)
    {
        var result = new DriveStatusResult
        {
            Configured = HasCredential(),
            ServiceAccountEmail = TryGetServiceAccountEmail()
        };

        if (!result.Configured)
        {
            result.Error = "No Google service account key configured (GoogleDrive:ServiceAccountKeyJson or GoogleDrive:ServiceAccountKeyPath).";
            return result;
        }

        if (string.IsNullOrWhiteSpace(folderId))
        {
            result.Error = "No Google Drive folder id configured in backup settings.";
            return result;
        }

        try
        {
            var service = GetDriveService();

            var getRequest = service.Files.Get(folderId);
            getRequest.SupportsAllDrives = true;
            getRequest.Fields = "id, name, mimeType";
            var folder = await getRequest.ExecuteAsync(cancellationToken);

            if (folder.MimeType != "application/vnd.google-apps.folder")
            {
                result.Error = $"'{folder.Name}' is not a folder.";
                return result;
            }

            // Listing confirms read access; upload permission is verified on first real backup
            var listRequest = service.Files.List();
            listRequest.Q = $"'{folderId}' in parents and trashed = false";
            listRequest.PageSize = 1;
            listRequest.SupportsAllDrives = true;
            listRequest.IncludeItemsFromAllDrives = true;
            await listRequest.ExecuteAsync(cancellationToken);

            result.Ok = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Drive validation failed for folder {FolderId}", folderId);
            result.Error = ex.Message;
        }

        return result;
    }

    private bool HasCredential() =>
        !string.IsNullOrWhiteSpace(_configuration["GoogleDrive:ServiceAccountKeyJson"]) ||
        !string.IsNullOrWhiteSpace(_configuration["GoogleDrive:ServiceAccountKeyPath"]);

    private string? TryGetServiceAccountEmail()
    {
        try
        {
            var json = GetCredentialJson();
            if (json == null)
                return null;

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("client_email", out var email) ? email.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private string? GetCredentialJson()
    {
        var inlineJson = _configuration["GoogleDrive:ServiceAccountKeyJson"];
        if (!string.IsNullOrWhiteSpace(inlineJson))
            return inlineJson;

        var keyPath = _configuration["GoogleDrive:ServiceAccountKeyPath"];
        if (!string.IsNullOrWhiteSpace(keyPath) && File.Exists(keyPath))
            return File.ReadAllText(keyPath);

        return null;
    }

    private DriveService GetDriveService()
    {
        if (_driveService != null)
            return _driveService;

        lock (_serviceLock)
        {
            if (_driveService != null)
                return _driveService;

            var json = GetCredentialJson()
                ?? throw new InvalidOperationException(
                    "Google Drive is not configured: set GoogleDrive:ServiceAccountKeyJson or GoogleDrive:ServiceAccountKeyPath.");

            var credential = GoogleCredential.FromJson(json)
                .CreateScoped(DriveService.Scope.DriveFile);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "SujanMotors AutoPartShop"
            });

            return _driveService;
        }
    }
}
