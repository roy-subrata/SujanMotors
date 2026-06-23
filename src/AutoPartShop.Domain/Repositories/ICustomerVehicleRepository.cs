






using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface ICustomerVehicleRepository : IBaseRepository<CustomerVehicle>
{
    /// <summary>
    /// Get all (non-deleted) vehicles owned by a customer.
    /// </summary>
    Task<IEnumerable<CustomerVehicle>> GetByCustomerAsync(Guid customerId, bool activeOnly = false, CancellationToken cancellationToken = default);
}
