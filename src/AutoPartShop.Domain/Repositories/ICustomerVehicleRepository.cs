






using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface ICustomerVehicleRepository : IBaseRepository<CustomerVehicle>
{
    /// <summary>
    /// Get all (non-deleted) vehicles owned by a customer.
    /// </summary>
    Task<IEnumerable<CustomerVehicle>> GetByCustomerAsync(Guid customerId, bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this customer already has a vehicle with the given registration number
    /// (plates can legitimately be reassigned across different customers over time, so
    /// this check is scoped to one customer rather than global).
    /// </summary>
    Task<bool> RegistrationExistsForCustomerAsync(Guid customerId, string registrationNo, Guid? excludeVehicleId = null, CancellationToken cancellationToken = default);
}
