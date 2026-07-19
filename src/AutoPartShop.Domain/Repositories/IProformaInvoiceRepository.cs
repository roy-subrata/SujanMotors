using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IProformaInvoiceRepository
{
    Task<ProformaInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProformaInvoice>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<ProformaInvoice> ProformaInvoices, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(ProformaInvoice entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProformaInvoice entity, CancellationToken cancellationToken = default);
}
