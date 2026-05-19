using AutoPartShop.Domain.Entities;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CustomerPaymentRepository(AutoPartDbContext _dbContext) : ICustomerPaymentRepository
{
    #region Base CRUD Operations

    public async Task<IEnumerable<CustomerPayment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.PaymentProvider)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Include(x => x.PaymentProvider)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(CustomerPayment entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.CustomerPayments.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CustomerPayment entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.CustomerPayments.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CustomerPayments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.CustomerPayments.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    #endregion

    #region Query Methods

    public async Task<CustomerPayment?> GetByTransactionNumberAsync(string transactionNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .FirstOrDefaultAsync(x => x.TransactionNumber == transactionNumber && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<CustomerPayment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Invoice)
            .Include(x => x.PaymentProvider)
            .Include(x => x.SourceAdvancePayment)
            .Where(x => x.CustomerId == customerId && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerPayment>> GetByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.PaymentProvider)
            .Where(x => x.InvoiceId == invoiceId && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerPayment>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.PaymentProvider)
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerPayment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.PaymentProvider)
            .Where(x => x.PaymentDate >= startDate && x.PaymentDate <= endDate && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerPayment>> GetByPaymentMethodAsync(string paymentMethod, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.PaymentProvider)
            .Where(x => x.PaymentMethod == paymentMethod && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerPayment>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.PaymentProvider)
            .Where(x => x.Status == "PENDING" && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerPayment>> GetFailedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.PaymentProvider)
            .Where(x => x.Status == "FAILED" && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Aggregate Methods

    public async Task<decimal> GetTotalByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Where(x => x.CustomerId == customerId && x.Status == "COMPLETED" && !x.Isdeleted)
            .SumAsync(x => x.Amount, cancellationToken);
    }

    public async Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Where(x => x.PaymentDate >= startDate && x.PaymentDate <= endDate && x.Status == "COMPLETED" && !x.Isdeleted)
            .SumAsync(x => x.Amount, cancellationToken);
    }

    #endregion

    #region Pagination Methods

    public async Task<(IEnumerable<CustomerPayment> payments, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Include(x => x.PaymentProvider)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToPagedTupleAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<(IEnumerable<CustomerPayment> payments, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();
        return await _dbContext.CustomerPayments
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Include(x => x.PaymentProvider)
            .Where(x => !x.Isdeleted && (
                x.TransactionNumber.ToLower().Contains(term) ||
                x.PaymentMethod.ToLower().Contains(term)
            ))
            .OrderByDescending(x => x.PaymentDate)
            .ToPagedTupleAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<(IEnumerable<CustomerPayment> payments, int totalCount)> GetByCustomerPagedAsync(Guid customerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerPayments
            .Include(x => x.Invoice)
            .Include(x => x.PaymentProvider)
            .Where(x => x.CustomerId == customerId && !x.Isdeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToPagedTupleAsync(pageNumber, pageSize, cancellationToken);
    }


    #endregion
}
