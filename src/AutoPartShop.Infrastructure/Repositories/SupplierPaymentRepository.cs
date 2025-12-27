using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class SupplierPaymentRepository(AutoPartDbContext _db) : ISupplierPaymentRepository
{
    public async Task<IEnumerable<SupplierPayment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPayments.Where(x => !x.Isdeleted).OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);
    }

    public async Task<SupplierPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPayments.FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(SupplierPayment entity, CancellationToken cancellationToken = default)
    {
        _db.SupplierPayments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SupplierPayment entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.SupplierPayments.FirstOrDefaultAsync(x => x.Id == entity.Id);
        if (existing != null)
        {
            existing.UpdateNotes(entity.Notes);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SupplierPayments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity != null)
            _db.SupplierPayments.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

    public async Task<SupplierPayment?> GetByTransactionNumberAsync(string transactionNumber, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.FirstOrDefaultAsync(x => x.TransactionNumber == transactionNumber && !x.Isdeleted, cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.SupplierId == supplierId && !x.Isdeleted).OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.PurchaseOrderId == purchaseOrderId && !x.Isdeleted).OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.Status == status && !x.Isdeleted).OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.PaymentDate >= startDate && x.PaymentDate <= endDate && !x.Isdeleted).OrderByDescending(x => x.PaymentDate).ToListAsync();

    public async Task<IEnumerable<SupplierPayment>> GetByPaymentMethodAsync(string paymentMethod, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.PaymentMethod == paymentMethod && !x.Isdeleted).OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetPendingAsync(CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.Status == "PENDING" && !x.Isdeleted).OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetFailedAsync(CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.Status == "FAILED" && !x.Isdeleted).OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);

    public async Task<decimal> GetTotalBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.SupplierId == supplierId && x.Status == "COMPLETED" && !x.Isdeleted).SumAsync(x => x.Amount);

    public async Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments.Where(x => x.PaymentDate >= startDate && x.PaymentDate <= endDate && x.Status == "COMPLETED" && !x.Isdeleted).SumAsync(x => x.Amount);


    public async Task<(IEnumerable<SupplierPayment> payments, int totalCount)> GetBySupplierPagedAsync(Guid supplierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = await GetBySupplierAsync(supplierId, cancellationToken);
        var paged = all.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        return (paged, all.Count());
    }

    public async Task<(IEnumerable<SupplierPayment> payments, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.SupplierPayments.Where(c => !c.Isdeleted);
        var totalCount = await query.CountAsync(cancellationToken);

        var parts = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (parts, totalCount);
    }
}
