using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

public class CostFloorEnforcementService : ICostFloorEnforcementService
{
    private readonly IApplicationSettingsRepository _settingsRepository;
    private readonly ICurrencyConversionService _currencyService;
    private readonly AutoPartDbContext _dbContext;

    public CostFloorEnforcementService(
        IApplicationSettingsRepository settingsRepository,
        ICurrencyConversionService currencyService,
        AutoPartDbContext dbContext)
    {
        _settingsRepository = settingsRepository;
        _currencyService = currencyService;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetMinMarginPercentsAsync(
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var value = await _settingsRepository.GetValueAsync("SALES_MIN_MARGIN_PERCENT", cancellationToken);
        var globalMinMargin = decimal.TryParse(value, out var parsed) ? parsed : 0m;

        var distinctIds = categoryIds.Distinct().ToList();
        var result = new Dictionary<Guid, decimal>(distinctIds.Count);
        if (distinctIds.Count == 0)
            return result;

        var overrides = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => distinctIds.Contains(c.Id) && c.MinMarginPercentOverride != null)
            .Select(c => new { c.Id, c.MinMarginPercentOverride })
            .ToDictionaryAsync(c => c.Id, c => c.MinMarginPercentOverride!.Value, cancellationToken);

        foreach (var id in distinctIds)
            result[id] = overrides.TryGetValue(id, out var categoryMargin) ? categoryMargin : globalMinMargin;

        return result;
    }

    public async Task<decimal> GetRateToBaseAsync(
        string orderCurrency,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default)
    {
        var converted = await _currencyService.ConvertToBaseWithRateAsync(1m, orderCurrency, effectiveDate, cancellationToken);
        return converted.RateToBase;
    }

    public async Task<CostFloorContext> PrepareContextAsync(
        IEnumerable<Guid> partIds,
        string currency,
        DateTime effectiveDate,
        Guid? approvalToken,
        CancellationToken cancellationToken = default)
    {
        var distinctPartIds = partIds.Distinct().ToList();
        var categoryIdByPart = await _dbContext.Parts
            .Where(p => distinctPartIds.Contains(p.Id))
            .Select(p => new { p.Id, p.CategoryId })
            .ToDictionaryAsync(p => p.Id, p => p.CategoryId, cancellationToken);

        var minMarginByCategory = await GetMinMarginPercentsAsync(categoryIdByPart.Values, cancellationToken);
        var rateToBase = await GetRateToBaseAsync(currency, effectiveDate, cancellationToken);
        var approval = await ResolveApprovalAsync(approvalToken, cancellationToken);

        return new CostFloorContext(categoryIdByPart, minMarginByCategory, rateToBase, approval);
    }

    public async Task<PriceOverrideApproval?> ResolveApprovalAsync(Guid? approvalToken, CancellationToken cancellationToken = default)
    {
        if (!approvalToken.HasValue || approvalToken.Value == Guid.Empty)
            return null;

        var approval = await _dbContext.PriceOverrideApprovals
            .FirstOrDefaultAsync(a => a.Id == approvalToken.Value, cancellationToken);

        return approval != null && approval.IsUsable(DateTime.UtcNow) ? approval : null;
    }

    public async Task MarkApprovalConsumedAsync(PriceOverrideApproval approval, string salesOrderNumber, CancellationToken cancellationToken = default)
    {
        approval.Consume(salesOrderNumber);
        _dbContext.PriceOverrideApprovals.Update(approval);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "SalesOrder",
            EntityId = salesOrderNumber,
            Action = "PRICE_OVERRIDE_APPROVED",
            PropertyName = "ApprovedBy",
            NewValue = approval.ApprovedByUsername,
            PerformedBy = approval.ApprovedByUsername,
            PerformedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogBlockedAttemptAsync(PriceOverrideRequiredException ex, string performedBy, CancellationToken cancellationToken = default)
    {
        // The transaction that would have contained this sale has already rolled back by the time
        // a controller catches PriceOverrideRequiredException — the DbContext may still hold that
        // failed attempt's entities as tracked-but-orphaned. Clear them so this save only persists
        // the audit row, not a resurrected partial sale.
        _dbContext.ChangeTracker.Clear();

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "SalesOrderLine",
            EntityId = ex.PartName,
            Action = ex.LimitType == "FLOOR" ? "PRICE_FLOOR_BLOCKED" : "PRICE_CEILING_BLOCKED",
            PropertyName = ex.LimitType == "FLOOR" ? "NetPrice" : "GrossPrice",
            OldValue = ex.Limit.ToString("F2"),
            NewValue = ex.AttemptedPrice.ToString("F2"),
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public bool EnforceLine(
        string partName,
        decimal netUnitPriceInOrderCurrency,
        decimal costPerBaseUnit,
        decimal minMarginPercent,
        decimal rateToBase,
        PriceOverrideApproval? approval)
    {
        return EnforceFloor(partName, netUnitPriceInOrderCurrency * rateToBase, costPerBaseUnit, minMarginPercent, approval);
    }

    public bool EnforceCeiling(
        string partName,
        decimal grossUnitPriceInOrderCurrency,
        decimal mrp,
        decimal rateToBase,
        PriceOverrideApproval? approval)
    {
        if (mrp <= 0)
            return false;

        var grossPriceInBaseCurrency = grossUnitPriceInOrderCurrency * rateToBase;
        if (grossPriceInBaseCurrency <= mrp + 0.01m)
            return false;

        if (approval != null)
            return true;

        throw new PriceOverrideRequiredException(
            "CEILING", partName, grossPriceInBaseCurrency, mrp,
            $"'{partName}' price ({grossPriceInBaseCurrency:F2}) is above the maximum allowed price (MRP {mrp:F2}). " +
            "Reduce the price, or get manager approval to complete this sale.");
    }

    public bool EnforceCartDiscount(
        IReadOnlyList<LineCostFloorCheck> lineChecks,
        decimal subTotal,
        decimal totalCartDiscount,
        decimal rateToBase,
        PriceOverrideApproval? approval)
    {
        if (totalCartDiscount <= 0 || subTotal <= 0)
            return false;

        var usedApproval = false;
        foreach (var check in lineChecks)
        {
            if (check.CostPerBaseUnit <= 0)
                continue;

            var lineShare = check.LineTotalPrice / subTotal;
            var cartDiscountForLine = totalCartDiscount * lineShare;
            var unitFactor = check.Quantity > 0 && check.QuantityInBaseUnit > 0 ? (decimal)check.QuantityInBaseUnit / check.Quantity : 1m;
            var additionalDiscountPerBaseUnit = unitFactor <= 0 ? 0 : (cartDiscountForLine / check.Quantity) / unitFactor;
            var finalNetUnitPriceInOrderCurrency = Math.Max(0, check.NetUnitPriceInOrderCurrency - additionalDiscountPerBaseUnit);
            if (EnforceLine(check.PartName, finalNetUnitPriceInOrderCurrency, check.CostPerBaseUnit, check.MinMarginPercent, rateToBase, approval))
                usedApproval = true;
        }

        return usedApproval;
    }

    private static bool EnforceFloor(
        string partName,
        decimal netUnitPriceInBaseCurrency,
        decimal costPerBaseUnit,
        decimal minMarginPercent,
        PriceOverrideApproval? approval)
    {
        if (costPerBaseUnit <= 0)
            return false;

        var floorPrice = costPerBaseUnit * (1 + minMarginPercent / 100);
        if (netUnitPriceInBaseCurrency >= floorPrice - 0.01m)
            return false;

        if (approval != null)
            return true;

        throw new PriceOverrideRequiredException(
            "FLOOR", partName, netUnitPriceInBaseCurrency, floorPrice,
            $"'{partName}' net price ({netUnitPriceInBaseCurrency:F2}) is below the minimum allowed price " +
            $"({floorPrice:F2}, cost {costPerBaseUnit:F2}). Reduce the discount, or get manager approval to complete this sale.");
    }
}
