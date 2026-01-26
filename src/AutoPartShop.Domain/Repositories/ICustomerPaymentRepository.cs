using AutoPartShop.Domain.Common;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

public interface ICustomerPaymentRepository : IBaseRepository<CustomerPayment>
{
    Task<CustomerPayment?> GetByTransactionNumberAsync(string transactionNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerPayment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerPayment>> GetByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerPayment>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerPayment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerPayment>> GetByPaymentMethodAsync(string paymentMethod, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerPayment>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerPayment>> GetFailedAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CustomerPayment> payments, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CustomerPayment> payments, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CustomerPayment> payments, int totalCount)> SearchPagedAsync(CustomerPaymentQuery query, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CustomerPayment> payments, int totalCount)> GetByCustomerPagedAsync(Guid customerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

public class CustomerPaymentQuery : BaseQuery
{
    public bool? IsReconciled { get; set; }
    public string? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}