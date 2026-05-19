using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class WarrantyRegistrationRepository(AutoPartDbContext _db) : IWarrantyRegistrationRepository
{
    public async Task<IEnumerable<WarrantyRegistration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations
            .Where(w => !w.Isdeleted)
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Include(w => w.SalesOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<WarrantyRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Include(w => w.SalesOrder)
            .Include(w => w.SalesOrderLine)
            .Include(w => w.Claims)
            .FirstOrDefaultAsync(w => w.Id == id && !w.Isdeleted, cancellationToken);
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
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Where(w => w.Status == "ACTIVE" && !w.Isdeleted)
            .OrderBy(w => w.WarrantyExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetExpiredWarrantiesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Where(w => w.Status == "EXPIRED" && !w.Isdeleted)
            .OrderByDescending(w => w.WarrantyExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyRegistration>> GetExpiringWarrantiesAsync(int daysFromNow, CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysFromNow);
        return await _db.WarrantyRegistrations
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .Where(w => w.Status == "ACTIVE"
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
            var normalizedStatus = status.ToUpper();
            query = query.Where(w => w.Status == normalizedStatus);
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
}
