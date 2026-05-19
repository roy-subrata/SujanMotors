using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface ISupplierRepository : IBaseRepository<Supplier>
{
    /// <summary>
    /// Get all active suppliers
    /// </summary>
    Task<IEnumerable<Supplier>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search suppliers with pagination
    /// </summary>
    Task<(IEnumerable<Supplier> Suppliers, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);


    /// <summary>
    /// Check if a supplier code already exists
    /// </summary>
    Task<bool> CodeExistsAsync(string code, Guid? excludeSupplierId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supplier by code
    /// </summary>
    Task<Supplier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}

