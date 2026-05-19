using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

/// <summary>
/// Repository interface for CreditNote entity
/// </summary>
public interface ICreditNoteRepository
{
    Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditNote>> GetBySupplierIdAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditNote>> GetAvailableCreditsAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CreditNote> CreditNotes, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CreditNote> CreditNotes, int TotalCount)> SearchPagedAsync(
        CreditNoteQuery query, CancellationToken cancellationToken = default);
    Task AddAsync(CreditNote entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(CreditNote entity, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalAvailableCreditAsync(Guid supplierId, CancellationToken cancellationToken = default);
}

public class CreditNoteQuery
{
    public Guid? SupplierId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
