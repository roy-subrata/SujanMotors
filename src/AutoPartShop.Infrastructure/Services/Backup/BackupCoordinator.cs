namespace AutoPartShop.Infrastructure.Services.Backup;

/// <summary>
/// Singleton guard ensuring only one backup or restore runs at a time
/// (across the scheduler and manually triggered API operations).
/// </summary>
public sealed class BackupCoordinator
{
    private readonly Lock _lock = new();
    private string? _currentOperation;

    /// <summary>
    /// The operation currently running ("backup" / "restore"), or null when idle
    /// </summary>
    public string? CurrentOperation
    {
        get { lock (_lock) return _currentOperation; }
    }

    /// <summary>
    /// Try to claim the coordinator for an operation. Returns false if another operation is running.
    /// </summary>
    public bool TryBegin(string operation)
    {
        lock (_lock)
        {
            if (_currentOperation != null)
                return false;

            _currentOperation = operation;
            return true;
        }
    }

    public void End()
    {
        lock (_lock)
        {
            _currentOperation = null;
        }
    }
}
