namespace AutoPartShop.Infrastructure.Repositories;

using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for BackupRecord entity
/// </summary>
public class BackupRecordRepository(AutoPartDbContext context) : IBackupRecordRepository
{
    private readonly AutoPartDbContext _context = context;

    /// <inheritdoc/>
    public async Task<IEnumerable<BackupRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BackupRecords
            .Where(b => !b.Isdeleted)
            .OrderByDescending(b => b.StartedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BackupRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BackupRecords
            .FirstOrDefaultAsync(b => b.Id == id && !b.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(BackupRecord entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _context.BackupRecords.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(BackupRecord entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _context.BackupRecords.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await GetByIdAsync(id, cancellationToken);
        if (record != null)
        {
            record.Prune();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BackupRecords
            .AnyAsync(b => b.Id == id && !b.Isdeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(List<BackupRecord> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BackupRecords.Where(b => !b.Isdeleted);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<List<BackupRecord>> GetRestorableBeyondRetentionAsync(
        int keepCount,
        CancellationToken cancellationToken = default)
    {
        return await _context.BackupRecords
            .Where(b => !b.Isdeleted &&
                        (b.Status == BackupRecord.Statuses.Succeeded ||
                         b.Status == BackupRecord.Statuses.UploadFailed))
            .OrderByDescending(b => b.StartedAt)
            .Skip(keepCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<BackupRecord>> GetInProgressAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BackupRecords
            .Where(b => !b.Isdeleted &&
                        (b.Status == BackupRecord.Statuses.Pending ||
                         b.Status == BackupRecord.Statuses.Running))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasScheduledRunSinceAsync(DateTime utcCutoff, CancellationToken cancellationToken = default)
    {
        // Includes soft-deleted (pruned) records: a pruned run still counts as "ran today"
        return await _context.BackupRecords
            .AnyAsync(b => b.TriggerType == BackupRecord.TriggerTypes.Scheduled && b.StartedAt >= utcCutoff,
                cancellationToken);
    }
}
