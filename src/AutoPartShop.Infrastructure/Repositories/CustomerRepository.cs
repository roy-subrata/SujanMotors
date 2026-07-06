using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AutoPartDbContext _dbContext;

    public CustomerRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.CustomerCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Customer entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var exists = await _dbContext.Customers
            .AnyAsync(x => x.CustomerCode == entity.CustomerCode && !x.Isdeleted, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Customer with code '{entity.CustomerCode}' already exists");

        await _dbContext.Customers.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Customer entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Customers.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Customers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.Customers.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<Customer?> GetByCodeAsync(string customerCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .FirstOrDefaultAsync(x => x.CustomerCode == customerCode.ToUpper() && !x.Isdeleted, cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .FirstOrDefaultAsync(x => x.Email == email.ToLower() && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderBy(x => x.CustomerCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByTypeAsync(string customerType, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Where(x => x.CustomerType == customerType.ToUpper() && !x.Isdeleted)
            .OrderBy(x => x.CustomerCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Where(x => x.City == city && !x.Isdeleted)
            .OrderBy(x => x.CustomerCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByCountryAsync(string country, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Where(x => x.Country == country && !x.Isdeleted)
            .OrderBy(x => x.CustomerCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();
        return await _dbContext.Customers
            .Where(x => !x.Isdeleted && (
                x.FirstName.ToLower().Contains(term) ||
                x.LastName.ToLower().Contains(term) ||
                x.CompanyName.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term) ||
                x.CustomerCode.ToLower().Contains(term)
            ))
            .OrderBy(x => x.CustomerCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetWithCreditLimitExceededAsync(CancellationToken cancellationToken = default)
    {
        // Credit limit feature removed - return empty list
        return await Task.FromResult(Enumerable.Empty<Customer>());
    }

    public async Task<IEnumerable<Customer>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .Where(x => x.Status == "ACTIVE" && !x.Isdeleted)
            .OrderBy(x => x.CustomerCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Customer> customers, int totalCount)> GetPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.CustomerCode);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
    public async Task<(IEnumerable<Customer> customers, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .Where(x => !x.Isdeleted)
            .OrderBy(x => x.CustomerCode);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }


    public async Task<IEnumerable<Customer>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.CreatedDate)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        return await _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .FirstOrDefaultAsync(x =>
                !x.Isdeleted &&
                (x.Phone == phone || x.AlternatePhone == phone),
                cancellationToken);
    }
}
