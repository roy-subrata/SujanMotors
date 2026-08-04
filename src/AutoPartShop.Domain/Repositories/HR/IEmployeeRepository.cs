using AutoPartShop.Domain.Entities.HR;
using AutoPartShop.Domain.Enums.HR;

namespace AutoPartShop.Domain.Repositories.HR;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Employee entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    // Specialized queries
    Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Employee>> GetByStatusAsync(EmployeeStatus status, CancellationToken cancellationToken = default);
}
