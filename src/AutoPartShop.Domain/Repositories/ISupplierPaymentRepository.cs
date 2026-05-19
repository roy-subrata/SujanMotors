using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

public interface ISupplierPaymentRepository : IBaseRepository<SupplierPayment>
{
    Task<SupplierPayment?> GetByTransactionNumberAsync(string transactionNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPayment>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPayment>> GetByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPayment>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPayment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPayment>> GetByPaymentMethodAsync(string paymentMethod, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPayment>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierPayment>> GetFailedAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<(IEnumerable<SupplierPayment> payments, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<SupplierPayment> payments, int totalCount)> GetBySupplierPagedAsync(Guid supplierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

}
