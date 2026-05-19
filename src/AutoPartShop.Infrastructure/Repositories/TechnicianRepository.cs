using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class TechnicianRepository : ITechnicianRepository
{
    private readonly AutoPartDbContext _dbContext;

    public TechnicianRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Technician>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Technicians
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Technician?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Technicians
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Technician entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var exists = await _dbContext.Technicians
            .AnyAsync(x => x.TechnicianCode == entity.TechnicianCode && !x.Isdeleted, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Technician with code '{entity.TechnicianCode}' already exists");

        await _dbContext.Technicians.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Technician entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Technicians.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Technicians
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.Technicians.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Technicians
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<Technician?> GetByCodeAsync(string technicianCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Technicians
            .FirstOrDefaultAsync(x => x.TechnicianCode == technicianCode && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Technician>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Technicians
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Technician> technicians, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Technicians
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Technician> technicians, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();
        var query = _dbContext.Technicians
            .Where(x => !x.Isdeleted && (
                x.TechnicianCode.ToLower().Contains(term) ||
                x.Name.ToLower().Contains(term) ||
                x.Phone.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term) ||
                x.ShopName.ToLower().Contains(term)
            ))
            .OrderBy(x => x.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
