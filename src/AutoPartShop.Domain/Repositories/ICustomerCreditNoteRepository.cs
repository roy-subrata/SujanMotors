using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

/// <summary>
/// Repository interface for CustomerCreditNote entity
/// </summary>
public interface ICustomerCreditNoteRepository
{
    Task<CustomerCreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerCreditNote>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerCreditNote>> GetAvailableCreditsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CustomerCreditNote> CreditNotes, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CustomerCreditNote> CreditNotes, int TotalCount)> SearchPagedAsync(
        CustomerCreditNoteQuery query, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerCreditNote entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomerCreditNote entity, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalAvailableCreditAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public class CustomerCreditNoteQuery
{
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
