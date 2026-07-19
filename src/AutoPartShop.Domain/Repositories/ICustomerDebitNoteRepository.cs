using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface ICustomerDebitNoteRepository
{
    Task<CustomerDebitNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDebitNote>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CustomerDebitNote> DebitNotes, int TotalCount)> SearchPagedAsync(
        CustomerDebitNoteQuery query, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerDebitNote entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomerDebitNote entity, CancellationToken cancellationToken = default);
}

public class CustomerDebitNoteQuery
{
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
