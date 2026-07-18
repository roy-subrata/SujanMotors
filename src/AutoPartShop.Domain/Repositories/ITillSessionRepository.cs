using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface ITillSessionRepository
{
    Task<TillSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TillSession?> GetOpenSessionForCashierAsync(Guid cashierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// This terminal's most recently CLOSED session, if any — used to suggest a next opening
    /// float. Scoped by terminal, not cashier: the cash physically stays in the drawer between
    /// shifts regardless of who's counting it next, so it's the drawer's history that matters,
    /// not the cashier's.
    /// </summary>
    Task<TillSession?> GetLastClosedSessionForTerminalAsync(string terminalLabel, CancellationToken cancellationToken = default);
    Task<(IEnumerable<TillSession> Sessions, int TotalCount)> SearchPagedAsync(
        TillSessionQuery query, CancellationToken cancellationToken = default);
    Task AddAsync(TillSession entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TillSession entity, CancellationToken cancellationToken = default);
}

public class TillSessionQuery
{
    public Guid? CashierId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
