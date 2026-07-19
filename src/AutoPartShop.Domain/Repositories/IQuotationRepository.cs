using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IQuotationRepository
{
    Task<Quotation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Quotation?> GetByNumberAsync(string quotationNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Quotation>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Quotation> Quotations, int TotalCount)> SearchPagedAsync(
        QuotationQuery query, CancellationToken cancellationToken = default);
    Task AddAsync(Quotation entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Quotation entity, CancellationToken cancellationToken = default);
}

public class QuotationQuery
{
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
