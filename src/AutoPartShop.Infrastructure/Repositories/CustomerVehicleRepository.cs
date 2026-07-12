using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CustomerVehicleRepository : ICustomerVehicleRepository
{
    private readonly AutoPartDbContext _dbContext;

    public CustomerVehicleRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CustomerVehicle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerVehicles
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerVehicle>> GetByCustomerAsync(Guid customerId, bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CustomerVehicles
            .Where(x => x.CustomerId == customerId && !x.Isdeleted);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerVehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerVehicles
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(CustomerVehicle entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.CustomerVehicles.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CustomerVehicle entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.CustomerVehicles.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CustomerVehicles
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.CustomerVehicles.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerVehicles
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
}
