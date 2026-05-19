using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface ITechnicianRepository
{
    Task<IEnumerable<Technician>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Technician?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Technician entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Technician entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    // Specialized queries
    Task<Technician?> GetByCodeAsync(string technicianCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Technician>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Technician> technicians, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Technician> technicians, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
