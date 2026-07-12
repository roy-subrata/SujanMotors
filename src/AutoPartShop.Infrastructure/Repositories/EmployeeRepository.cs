using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AutoPartDbContext _dbContext;

    public EmployeeRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Employee entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var exists = await _dbContext.Employees
            .AnyAsync(x => x.EmployeeCode == entity.EmployeeCode && !x.Isdeleted, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Employee with code '{entity.EmployeeCode}' already exists");

        await _dbContext.Employees.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Employee entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Employees.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

        if (entity != null)
        {
            entity.Isdeleted = true;
            // Free the login account so it can back a future employee record
            entity.UnlinkUserAccount();
            _dbContext.Employees.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.EmployeeCode == employeeCode && !x.Isdeleted, cancellationToken);
    }

    public async Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Employee>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
