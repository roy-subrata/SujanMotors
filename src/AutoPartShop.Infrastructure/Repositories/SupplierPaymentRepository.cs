using AutoPartShop.Domain.Entities;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class SupplierPaymentRepository(AutoPartDbContext _db) : ISupplierPaymentRepository
{
    #region Base CRUD Operations

    public async Task<IEnumerable<SupplierPayment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
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

    #endregion

    #region Query Methods

    public async Task<SupplierPayment?> GetByTransactionNumberAsync(string transactionNumber, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .FirstOrDefaultAsync(x => x.TransactionNumber == transactionNumber && !x.Isdeleted, cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Include(x => x.SourceAdvancePayment)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.GoodsReceipt)
            .Where(x => x.SupplierId == supplierId && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Where(x => x.PurchaseOrderId == purchaseOrderId && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Where(x => x.PaymentDate >= startDate && x.PaymentDate <= endDate && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetByPaymentMethodAsync(string paymentMethod, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Where(x => x.PaymentMethod == paymentMethod && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetPendingAsync(CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Where(x => x.Status == "PENDING" && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<SupplierPayment>> GetFailedAsync(CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Where(x => x.Status == "FAILED" && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);

    #endregion

    #region Aggregate Methods

    public async Task<decimal> GetTotalBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Where(x => x.SupplierId == supplierId && x.Status == "COMPLETED" && !x.Isdeleted)
            .SumAsync(x => x.Amount, cancellationToken);

    public async Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        => await _db.SupplierPayments
            .Where(x => x.PaymentDate >= startDate && x.PaymentDate <= endDate && x.Status == "COMPLETED" && !x.Isdeleted)
            .SumAsync(x => x.Amount, cancellationToken);

    #endregion

    #region Pagination Methods

    public async Task<(IEnumerable<SupplierPayment> payments, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToPagedTupleAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<(IEnumerable<SupplierPayment> payments, int totalCount)> GetBySupplierPagedAsync(Guid supplierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _db.SupplierPayments
            .Include(x => x.PaymentProvider)
            .Include(x => x.SupplierPaymentAccount)
            .Include(x => x.SourceAdvancePayment)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.GoodsReceipt)
            .Where(x => x.SupplierId == supplierId && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToPagedTupleAsync(pageNumber, pageSize, cancellationToken);
    }



    #endregion

}
