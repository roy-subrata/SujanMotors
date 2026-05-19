using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class WarrantyClaimRepository(AutoPartDbContext _db) : IWarrantyClaimRepository
{
    public async Task<IEnumerable<WarrantyClaim>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaims
            .Where(wc => !wc.Isdeleted)
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.Customer)
            .Include(wc => wc.Technician)
            .OrderByDescending(wc => wc.ClaimDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<WarrantyClaim?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaims
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.SalesOrder)
            .Include(wc => wc.Customer)
            .Include(wc => wc.Technician)
            .FirstOrDefaultAsync(wc => wc.Id == id && !wc.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(WarrantyClaim entity, CancellationToken cancellationToken = default)
    {
        _db.WarrantyClaims.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WarrantyClaim entity, CancellationToken cancellationToken = default)
    {
        _db.WarrantyClaims.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var claim = await _db.WarrantyClaims.FirstOrDefaultAsync(wc => wc.Id == id, cancellationToken);
        if (claim != null)
        {
            _db.WarrantyClaims.Remove(claim);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaims.AnyAsync(wc => wc.Id == id && !wc.Isdeleted, cancellationToken);
    }

    public async Task<WarrantyClaim?> GetByClaimNumberAsync(string claimNumber, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = claimNumber.ToUpper().Trim();
        return await _db.WarrantyClaims
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.Customer)
            .Include(wc => wc.Technician)
            .FirstOrDefaultAsync(wc => wc.ClaimNumber == normalizedNumber && !wc.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<WarrantyClaim>> GetByWarrantyRegistrationIdAsync(Guid warrantyRegistrationId, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaims
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.Customer)
            .Include(wc => wc.Technician)
            .Where(wc => wc.WarrantyRegistrationId == warrantyRegistrationId && !wc.Isdeleted)
            .OrderByDescending(wc => wc.ClaimDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyClaim>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaims
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.Technician)
            .Where(wc => wc.CustomerId == customerId && !wc.Isdeleted)
            .OrderByDescending(wc => wc.ClaimDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyClaim>> GetByTechnicianIdAsync(Guid technicianId, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaims
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.Customer)
            .Where(wc => wc.TechnicianId == technicianId && !wc.Isdeleted)
            .OrderByDescending(wc => wc.ClaimDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyClaim>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = status.ToUpper();
        return await _db.WarrantyClaims
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.Customer)
            .Include(wc => wc.Technician)
            .Where(wc => wc.Status == normalizedStatus && !wc.Isdeleted)
            .OrderByDescending(wc => wc.ClaimDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WarrantyClaim>> GetPendingClaimsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync("PENDING", cancellationToken);
    }

    public async Task<IEnumerable<WarrantyClaim>> GetInProgressClaimsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync("IN_PROGRESS", cancellationToken);
    }

    public async Task<IEnumerable<WarrantyClaim>> GetOpenClaimsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaims
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.Customer)
            .Include(wc => wc.Technician)
            .Where(wc => wc.Status != "CLOSED" && wc.Status != "REJECTED" && !wc.Isdeleted)
            .OrderByDescending(wc => wc.ClaimDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<WarrantyClaim> Claims, int TotalCount)> SearchPagedAsync(
        string? searchTerm,
        string? status,
        string? serviceType,
        Guid? customerId,
        Guid? technicianId,
        Guid? warrantyRegistrationId,
        DateTime? claimDateFrom,
        DateTime? claimDateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WarrantyClaims
            .Include(wc => wc.WarrantyRegistration)
                .ThenInclude(wr => wr!.Part)
            .Include(wc => wc.Customer)
            .Include(wc => wc.Technician)
            .Where(wc => !wc.Isdeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(wc =>
                wc.ClaimNumber.ToLower().Contains(term) ||
                wc.IssueDescription.ToLower().Contains(term) ||
                wc.Customer!.FirstName.ToLower().Contains(term) ||
                wc.Customer!.LastName.ToLower().Contains(term) ||
                (wc.WarrantyRegistration != null && wc.WarrantyRegistration.WarrantyNumber.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.ToUpper();
            query = query.Where(wc => wc.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(serviceType))
        {
            var normalizedServiceType = serviceType.ToUpper();
            query = query.Where(wc => wc.ServiceType == normalizedServiceType);
        }

        if (customerId.HasValue)
        {
            query = query.Where(wc => wc.CustomerId == customerId.Value);
        }

        if (technicianId.HasValue)
        {
            query = query.Where(wc => wc.TechnicianId == technicianId.Value);
        }

        if (warrantyRegistrationId.HasValue)
        {
            query = query.Where(wc => wc.WarrantyRegistrationId == warrantyRegistrationId.Value);
        }

        if (claimDateFrom.HasValue)
        {
            query = query.Where(wc => wc.ClaimDate >= claimDateFrom.Value);
        }

        if (claimDateTo.HasValue)
        {
            query = query.Where(wc => wc.ClaimDate <= claimDateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var claims = await query
            .OrderByDescending(wc => wc.ClaimDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (claims, totalCount);
    }

    public async Task<bool> ClaimNumberExistsAsync(string claimNumber, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = claimNumber.ToUpper().Trim();
        return await _db.WarrantyClaims
            .AnyAsync(wc => wc.ClaimNumber == normalizedNumber && !wc.Isdeleted, cancellationToken);
    }

    public async Task<decimal> GetTotalServiceCostByWarrantyAsync(Guid warrantyRegistrationId, CancellationToken cancellationToken = default)
    {
        return await _db.WarrantyClaims
            .Where(wc => wc.WarrantyRegistrationId == warrantyRegistrationId && !wc.Isdeleted)
            .SumAsync(wc => wc.ServiceCost, cancellationToken);
    }
}
