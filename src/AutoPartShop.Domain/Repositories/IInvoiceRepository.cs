using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;



public interface IInvoiceRepository : IBaseRepository<Invoice>
{
    Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Invoice> invoices, int totalCount)> GetPagedAsync(int pageNumber, int pageSize,
        string? searchTerm = null,
        string? status = null,
        Guid? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool hasDue = false,
        CancellationToken cancellationToken = default);
}
