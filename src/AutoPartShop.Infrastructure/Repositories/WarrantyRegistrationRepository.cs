using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class WarrantyRegistrationRepository(AutoPartDbContext _db) : IWarrantyRegistrationRepository
{
    public async Task<IEnumerable<WarrantyRegistration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.WarrantyRegistrations
            .Where(w => !w.Isdeleted)
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Include(w => w.SalesOrder)
            .ToListAsync(cancellationToken);

        await SweepExpiredStatusAsync(list, cancellationToken);
        return list;
    }

    public async Task<WarrantyRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var w = await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Include(w => w.SalesOrder)
            .Include(w => w.SalesOrderLine)
            .Include(w => w.Claims)
            .FirstOrDefaultAsync(w => w.Id == id && !w.Isdeleted, cancellationToken);

        if (w is not null)
        {
            var previous = w.Status;
            w.CheckAndUpdateExpiry();
            if (w.Status != previous) await _db.SaveChangesAsync(cancellationToken);
        }

        return w;
    }

    public async Task AddAsync(WarrantyRegistration entity, CancellationToken cancellationToken = default)
    {
        _db.WarrantyRegistrations.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WarrantyRegistration entity, CancellationToken cancellationToken = default)
    {
        _db.WarrantyRegistrations.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warranty = await _db.WarrantyRegistrations.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warranty != null)
        {
            _db.WarrantyRegistrations.Remove(warranty);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations.AnyAsync(w => w.Id == id && !w.Isdeleted, cancellationToken);
    }

    public async Task<WarrantyRegistration?> GetByWarrantyNumberAsync(string warrantyNumber, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = warrantyNumber.ToUpper().Trim();
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Include(w => w.SalesOrder)
            .Include(w => w.SalesOrderLine)
            .Include(w => w.Claims)
            .FirstOrDefaultAsync(w => w.WarrantyNumber == normalizedNumber && !w.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.SalesOrder)
            .Where(w => w.CustomerId == customerId && !w.Isdeleted)
            .OrderByDescending(w => w.WarrantyStartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetBySalesOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Where(w => w.SalesOrderId == salesOrderId && !w.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations
            .Include(w => w.Customer)
            .Include(w => w.SalesOrder)
            .Where(w => w.PartId == partId && !w.Isdeleted)
            .OrderByDescending(w => w.WarrantyStartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetActiveWarrantiesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Where(w => w.Status == WarrantyRegistrationStatus.ACTIVE && !w.Isdeleted)
            .OrderBy(w => w.WarrantyExpiryDate)
            .ToListAsync(cancellationToken);

        await SweepExpiredStatusAsync(list, cancellationToken);
        return list.Where(w => w.Status == WarrantyRegistrationStatus.ACTIVE);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetExpiredWarrantiesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Where(w => w.Status == WarrantyRegistrationStatus.EXPIRED && !w.Isdeleted)
            .OrderByDescending(w => w.WarrantyExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetDueForExpiryAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations
            .Where(w => w.Status == WarrantyRegistrationStatus.ACTIVE
                && w.WarrantyExpiryDate < asOf
                && !w.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetExpiringWarrantiesAsync(int daysFromNow, CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysFromNow);
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Where(w => w.Status == WarrantyRegistrationStatus.ACTIVE
                && w.WarrantyExpiryDate <= expiryThreshold
                && w.WarrantyExpiryDate >= DateTime.UtcNow
                && !w.Isdeleted)
            .OrderBy(w => w.WarrantyExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<WarrantyRegistration> Warranties, int TotalCount)> SearchPagedAsync(
        string? searchTerm,
        string? status,
        Guid? customerId,
        Guid? partId,
        DateTime? expiryDateFrom,
        DateTime? expiryDateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Include(w => w.SalesOrder)
            .Where(w => !w.Isdeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(w =>
                w.WarrantyNumber.ToLower().Contains(term) ||
                w.CertificateNumber.ToLower().Contains(term) ||
                w.Part!.Name.ToLower().Contains(term) ||
                w.Customer!.FirstName.ToLower().Contains(term) ||
                w.Customer!.LastName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = Enum.TryParse<WarrantyRegistrationStatus>(status.Trim().ToUpperInvariant(), out var normalizedStatus)
                ? query.Where(w => w.Status == normalizedStatus)
                : query.Where(w => false);
        }

        if (customerId.HasValue)
        {
            query = query.Where(w => w.CustomerId == customerId.Value);
        }

        if (partId.HasValue)
        {
            query = query.Where(w => w.PartId == partId.Value);
        }

        if (expiryDateFrom.HasValue)
        {
            query = query.Where(w => w.WarrantyExpiryDate >= expiryDateFrom.Value);
        }

        if (expiryDateTo.HasValue)
        {
            query = query.Where(w => w.WarrantyExpiryDate <= expiryDateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var warranties = await query
            .OrderByDescending(w => w.WarrantyStartDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (warranties, totalCount);
    }

    public async Task<bool> WarrantyNumberExistsAsync(string warrantyNumber, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = warrantyNumber.ToUpper().Trim();
        return await _db.WarrantyRegistrations
            .AnyAsync(w => w.WarrantyNumber == normalizedNumber && !w.Isdeleted, cancellationToken);
    }

    /// <summary>
    /// Flips ACTIVE registrations whose expiry has passed to EXPIRED on read, so /active and
    /// /expired stay truthful without waiting for someone to call check-expiry. Only saves when
    /// a status actually changed — a plain list read must not write.
    /// </summary>
    private async Task SweepExpiredStatusAsync(List<WarrantyRegistration> warranties, CancellationToken cancellationToken)
    {
        var changed = false;
        foreach (var w in warranties)
        {
            var previous = w.Status;
            w.CheckAndUpdateExpiry();
            if (w.Status != previous) changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync(cancellationToken);
    }
}
