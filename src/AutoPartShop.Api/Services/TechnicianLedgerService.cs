using AutoPartShop.Application.DTOs.LedgerDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for calculating and retrieving technician ledger data.
/// Derives ledger entries from SalesOrders and their associated payments/returns
/// where the technician has temporary payment responsibility (PaymentResponsibility == "TECHNICIAN_TEMPORARY").
/// No separate database table is needed — the ledger is computed from existing transactional data.
/// </summary>
public class TechnicianLedgerService : ITechnicianLedgerService
{
    private readonly ITechnicianRepository _technicianRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<TechnicianLedgerService> _logger;

    public TechnicianLedgerService(
        ITechnicianRepository technicianRepository,
        AutoPartDbContext dbContext,
        ILogger<TechnicianLedgerService> logger)
    {
        _technicianRepository = technicianRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TechnicianLedgerSummaryDto> GetLedgerSummaryAsync(
        Guid technicianId, int entryLimit = 20, CancellationToken ct = default)
    {
        var technician = await _technicianRepository.GetByIdAsync(technicianId, ct);
        if (technician == null)
            throw new InvalidOperationException($"Technician with ID {technicianId} not found");

        var totalSales = await GetTotalSalesAsync(technicianId, ct);
        var totalPayments = await GetTotalPaymentsAsync(technicianId, ct);
        var totalReturns = await GetTotalReturnsAsync(technicianId, ct);
        var currentBalance = totalSales - totalPayments - totalReturns;

        var entries = await GetLedgerEntriesAsync(technicianId, null, null, ct);
        CalculateRunningBalances(entries);
        var recentEntries = entries.OrderByDescending(e => e.TransactionDate).Take(entryLimit).ToList();

        var orderCount = await _dbContext.SalesOrders
            .AsNoTracking()
            .CountAsync(so => !so.Isdeleted && so.TechnicianId == technicianId, ct);

        return new TechnicianLedgerSummaryDto
        {
            TechnicianId = technicianId,
            TechnicianName = technician.Name,
            TechnicianCode = technician.TechnicianCode,
            TotalSales = totalSales,
            TotalPayments = totalPayments,
            TotalReturns = totalReturns,
            CurrentBalance = currentBalance,
            TransactionCount = entries.Count,
            OrderCount = orderCount,
            LastTransactionDate = entries.MaxBy(e => e.TransactionDate)?.TransactionDate,
            Entries = recentEntries
        };
    }

    public async Task<decimal> CalculateCurrentBalanceAsync(Guid technicianId, CancellationToken ct = default)
    {
        var totalSales = await GetTotalSalesAsync(technicianId, ct);
        var totalPayments = await GetTotalPaymentsAsync(technicianId, ct);
        var totalReturns = await GetTotalReturnsAsync(technicianId, ct);
        return totalSales - totalPayments - totalReturns;
    }

    public async Task<PagedTechnicianLedgerResult> GetLedgerEntriesAsync(
        TechnicianLedgerQueryDto query, CancellationToken ct = default)
    {
        var allEntries = await GetLedgerEntriesAsync(
            query.TechnicianId, query.FromDate, query.ToDate, ct);

        if (query.TransactionType.HasValue)
        {
            allEntries = allEntries
                .Where(e => e.TransactionType == query.TransactionType.Value)
                .ToList();
        }

        allEntries = allEntries.OrderByDescending(e => e.TransactionDate).ToList();
        CalculateRunningBalances(allEntries);

        var totalCount = allEntries.Count;
        var pagedEntries = allEntries
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedTechnicianLedgerResult
        {
            Entries = pagedEntries,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<List<TechnicianLedgerEntryDto>> GetLedgerEntriesAsync(
        Guid technicianId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var entries = new List<TechnicianLedgerEntryDto>();

        entries.AddRange(await GetSaleEntriesAsync(technicianId, fromDate, toDate, ct));
        entries.AddRange(await GetReturnEntriesAsync(technicianId, fromDate, toDate, ct));
        entries.AddRange(await GetPaymentEntriesAsync(technicianId, fromDate, toDate, ct));

        CalculateRunningBalances(entries);
        return entries;
    }

    public async Task<decimal> GetTotalSalesAsync(Guid technicianId, CancellationToken ct = default)
    {
        return await _dbContext.SalesOrders
            .AsNoTracking()
            .Where(so => !so.Isdeleted
                && so.TechnicianId == technicianId
                && so.Status != SalesOrderStatus.CANCELLED)
            .SumAsync(so => (decimal?)so.GrandTotal, ct) ?? 0;
    }

    public async Task<decimal> GetTotalPaymentsAsync(Guid technicianId, CancellationToken ct = default)
    {
        return await _dbContext.CustomerPayments
            .AsNoTracking()
            .Where(p => !p.Isdeleted
                && p.Invoice != null
                && p.Invoice.SalesOrder != null
                && p.Invoice.SalesOrder.TechnicianId == technicianId
                && p.Status == CustomerPaymentStatus.COMPLETED
                && p.PaymentMethod != "CREDIT_NOTE")
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
    }

    public async Task<decimal> GetTotalReturnsAsync(Guid technicianId, CancellationToken ct = default)
    {
        return await _dbContext.Set<SalesReturn>()
            .AsNoTracking()
            .Where(r => !r.Isdeleted
                && r.Status == SalesReturnStatus.PROCESSED
                && r.SalesOrder != null
                && r.SalesOrder.TechnicianId == technicianId)
            .SumAsync(r => (decimal?)r.RefundAmount, ct) ?? 0;
    }

    #region Private Helper Methods

    private async Task<List<TechnicianLedgerEntryDto>> GetSaleEntriesAsync(
        Guid technicianId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _dbContext.SalesOrders
            .AsNoTracking()
            .Where(so => !so.Isdeleted
                && so.TechnicianId == technicianId
                && so.Status != SalesOrderStatus.CANCELLED);

        if (fromDate.HasValue)
            query = query.Where(so => so.SODate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(so => so.SODate <= toDate.Value);

        var orders = await query.ToListAsync(ct);

        return orders.Select(so => new TechnicianLedgerEntryDto
        {
            Id = so.Id,
            TransactionDate = so.SODate,
            TransactionType = TechnicianLedgerTransactionType.SALE,
            ReferenceNumber = so.SONumber,
            ReferenceId = so.Id,
            CustomerName = so.CustomerName,
            DebitAmount = so.GrandTotal,
            CreditAmount = 0,
            Description = $"Sale - {so.SONumber}",
            Status = so.Status.ToString()
        }).ToList();
    }

    private async Task<List<TechnicianLedgerEntryDto>> GetPaymentEntriesAsync(
        Guid technicianId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _dbContext.CustomerPayments
            .AsNoTracking()
            .Where(p => !p.Isdeleted
                && p.Invoice != null
                && p.Invoice.SalesOrder != null
                && p.Invoice.SalesOrder.TechnicianId == technicianId
                && p.Status == CustomerPaymentStatus.COMPLETED
                && p.PaymentMethod != "CREDIT_NOTE");

        if (fromDate.HasValue)
            query = query.Where(p => p.PaymentDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(p => p.PaymentDate <= toDate.Value);

        var payments = await query.ToListAsync(ct);

        return payments.Select(p => new TechnicianLedgerEntryDto
        {
            Id = p.Id,
            TransactionDate = p.PaymentDate,
            TransactionType = TechnicianLedgerTransactionType.PAYMENT,
            ReferenceNumber = p.TransactionNumber,
            ReferenceId = p.Id,
            CustomerName = null,
            DebitAmount = 0,
            CreditAmount = p.Amount,
            Description = $"Payment - {p.PaymentMethod}",
            Status = p.Status.ToString()
        }).ToList();
    }

    private async Task<List<TechnicianLedgerEntryDto>> GetReturnEntriesAsync(
        Guid technicianId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _dbContext.Set<SalesReturn>()
            .AsNoTracking()
            .Where(r => !r.Isdeleted
                && r.Status == SalesReturnStatus.PROCESSED
                && r.SalesOrder != null
                && r.SalesOrder.TechnicianId == technicianId);

        if (fromDate.HasValue)
            query = query.Where(r => r.ReturnDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(r => r.ReturnDate <= toDate.Value);

        var returns = await query.ToListAsync(ct);

        return returns.Select(r => new TechnicianLedgerEntryDto
        {
            Id = r.Id,
            TransactionDate = r.ApprovedDate ?? r.ReturnDate,
            TransactionType = TechnicianLedgerTransactionType.RETURN,
            ReferenceNumber = r.ReturnNumber,
            ReferenceId = r.Id,
            CustomerName = null,
            DebitAmount = 0,
            CreditAmount = r.RefundAmount,
            Description = $"Return - {r.Reason}",
            Status = r.Status.ToString()
        }).ToList();
    }

    private static void CalculateRunningBalances(List<TechnicianLedgerEntryDto> entries)
    {
        if (!entries.Any()) return;

        var sortedEntries = entries.OrderBy(e => e.TransactionDate).ToList();

        decimal runningBalance = 0;
        foreach (var entry in sortedEntries)
        {
            runningBalance += entry.DebitAmount - entry.CreditAmount;
            entry.RunningBalance = runningBalance;
        }

        var finalOrder = entries.OrderByDescending(e => e.TransactionDate).ToList();
        entries.Clear();
        entries.AddRange(finalOrder);
    }

    #endregion
}
