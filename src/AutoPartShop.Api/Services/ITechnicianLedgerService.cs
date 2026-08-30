using AutoPartShop.Application.DTOs.LedgerDtos;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for calculating and retrieving technician ledger data.
/// Derives entries from SalesOrders and their associated payments/returns
/// where the technician has temporary payment responsibility.
/// </summary>
public interface ITechnicianLedgerService
{
    Task<TechnicianLedgerSummaryDto> GetLedgerSummaryAsync(Guid technicianId, int entryLimit = 20, CancellationToken ct = default);
    Task<decimal> CalculateCurrentBalanceAsync(Guid technicianId, CancellationToken ct = default);
    Task<PagedTechnicianLedgerResult> GetLedgerEntriesAsync(TechnicianLedgerQueryDto query, CancellationToken ct = default);
    Task<List<TechnicianLedgerEntryDto>> GetLedgerEntriesAsync(Guid technicianId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    Task<decimal> GetTotalSalesAsync(Guid technicianId, CancellationToken ct = default);
    Task<decimal> GetTotalPaymentsAsync(Guid technicianId, CancellationToken ct = default);
    Task<decimal> GetTotalReturnsAsync(Guid technicianId, CancellationToken ct = default);
}
