using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ChallanRepository(AutoPartDbContext _db) : IChallanRepository
{
    public async Task<IEnumerable<Challan>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Challans.Include(c => c.Lines).Where(c => !c.Isdeleted)
            .OrderByDescending(c => c.CreatedDate).ToListAsync(ct);

    public async Task<Challan?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Challans
            .Include(c => c.Lines)
            .Include(c => c.SalesOrder)
            .Include(c => c.Invoice)
            .FirstOrDefaultAsync(c => c.Id == id && !c.Isdeleted, ct);

    public async Task<Challan?> GetByNumberAsync(string number, CancellationToken ct = default) =>
        await _db.Challans
            .Include(c => c.Lines)
            .Include(c => c.SalesOrder)
            .FirstOrDefaultAsync(c => c.ChallanNumber == number && !c.Isdeleted, ct);

    public async Task<IEnumerable<Challan>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken ct = default) =>
        await _db.Challans
            .Include(c => c.Lines)
            .Where(c => c.SalesOrderId == salesOrderId && !c.Isdeleted)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<Challan>> GetByStatusAsync(string status, CancellationToken ct = default) =>
        await _db.Challans
            .Include(c => c.SalesOrder)
            .Include(c => c.Lines)
            .Where(c => c.Status == status && !c.Isdeleted)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync(ct);

    public async Task<bool> HasPendingChallanAsync(Guid salesOrderId, CancellationToken ct = default) =>
        await _db.Challans.AnyAsync(
            c => c.SalesOrderId == salesOrderId && c.Status != "DELIVERED" && !c.Isdeleted, ct);

    public async Task AddAsync(Challan entity, CancellationToken ct = default)
    {
        await _db.Challans.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Challan entity, CancellationToken ct = default)
    {
        _db.Challans.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Challans.FindAsync(id, ct);
        if (entity != null) { entity.Isdeleted = true; await _db.SaveChangesAsync(ct); }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await _db.Challans.AnyAsync(c => c.Id == id && !c.Isdeleted, ct);
}
