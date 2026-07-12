using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.StockDtos;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Stock take / cycle count: snapshot expected quantities, record physical counts, review
/// variances, then approve — which applies every variance as a stock adjustment (level + lots)
/// in one transaction referencing the stock take number.
/// </summary>
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[Authorize(Roles = "Admin,Manager")]
public class StockTakeController : ControllerBase
{
    private readonly AutoPartDbContext _dbContext;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly StockAdjustmentApplier _adjustmentApplier;
    private readonly ILogger<StockTakeController> _logger;

    public StockTakeController(
        AutoPartDbContext dbContext,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        StockAdjustmentApplier adjustmentApplier,
        ILogger<StockTakeController> logger)
    {
        _dbContext = dbContext;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _adjustmentApplier = adjustmentApplier;
        _logger = logger;
    }

    /// <summary>
    /// Starts a stock take: snapshots the expected (base-unit) on-hand quantity of every active
    /// stock level in the warehouse — including zero-stock lines so "found" items get counted —
    /// optionally scoped to one category for cycle counting.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockTakeRequest request, CancellationToken cancellationToken)
    {
        if (request.WarehouseId == Guid.Empty)
            return BadRequest(new { message = "WarehouseId is required" });

        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && !w.Isdeleted, cancellationToken);
        if (warehouse == null)
            return NotFound(new { message = "Warehouse not found" });

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && !c.Isdeleted, cancellationToken);
            if (!categoryExists)
                return NotFound(new { message = "Category not found" });
        }

        // One open count per warehouse: two overlapping counts would both snapshot and later
        // both adjust the same stock levels, double-applying variances.
        var openTake = await _dbContext.StockTakes
            .Where(st => st.WarehouseId == request.WarehouseId
                && (st.Status == "COUNTING" || st.Status == "REVIEW") && !st.Isdeleted)
            .Select(st => st.StockTakeNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (openTake != null)
            return Conflict(new { message = $"Stock take {openTake} is still open for this warehouse. Complete or cancel it first." });

        try
        {
            var levelsQuery = _dbContext.StockLevels
                .Include(sl => sl.Part)
                .Include(sl => sl.Variant)
                .Where(sl => sl.WarehouseId == request.WarehouseId && sl.IsActive && !sl.Isdeleted
                    && sl.Part != null && !sl.Part.Isdeleted);

            if (request.CategoryId.HasValue)
                levelsQuery = levelsQuery.Where(sl => sl.Part!.CategoryId == request.CategoryId.Value);

            var levels = await levelsQuery.ToListAsync(cancellationToken);
            if (levels.Count == 0)
                return BadRequest(new { message = "No active stock levels found to count for this warehouse/category" });

            // Latest available lot cost per (part, variant) — values the variance at review time.
            var lots = await _dbContext.StockLots
                .Where(l => l.WarehouseId == request.WarehouseId && l.Status == "AVAILABLE"
                    && l.IsActive && !l.Isdeleted)
                .OrderByDescending(l => l.ReceivingDate)
                .Select(l => new { l.PartId, l.VariantId, l.CostPriceInBaseUnit, l.QuantityAvailableInBaseUnit })
                .ToListAsync(cancellationToken);

            decimal CostFor(Guid partId, Guid? variantId) =>
                lots.Where(l => l.PartId == partId && l.VariantId == variantId && l.QuantityAvailableInBaseUnit > 0)
                    .Select(l => (decimal?)l.CostPriceInBaseUnit).FirstOrDefault()
                ?? lots.Where(l => l.PartId == partId && l.VariantId == variantId)
                    .Select(l => (decimal?)l.CostPriceInBaseUnit).FirstOrDefault()
                ?? 0m;

            var username = _currentUserService.GetCurrentUsername();
            var number = await _codeGenerateService.GenerateAsync("ST", cancellationToken);

            var stockTake = StockTake.Create(number, request.WarehouseId, request.CategoryId, request.Notes);
            stockTake.CreatedBy = username;
            stockTake.ModifiedBy = username;

            foreach (var level in levels)
            {
                var line = StockTakeLine.Create(
                    stockTake.Id,
                    level.Id,
                    level.PartId,
                    level.VariantId,
                    level.Part?.Name ?? string.Empty,
                    level.Part?.PartNumber?.Value ?? level.Part?.SKU ?? string.Empty,
                    level.Variant?.Name ?? string.Empty,
                    level.Location,
                    level.QuantityOnHandInBaseUnit,
                    CostFor(level.PartId, level.VariantId));
                line.CreatedBy = username;
                line.ModifiedBy = username;
                stockTake.Lines.Add(line);
            }

            await _dbContext.StockTakes.AddAsync(stockTake, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = stockTake.Id }, MapHeader(stockTake, warehouse.Name, string.Empty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock take for warehouse {WarehouseId}", request.WarehouseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the stock take" });
        }
    }

    /// <summary>Paged stock take list, newest first. Optional status / warehouse / search filters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.StockTakes
            .Include(st => st.Warehouse)
            .Include(st => st.Category)
            .Where(st => !st.Isdeleted);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToUpper();
            query = query.Where(st => st.Status == normalized);
        }

        if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
            query = query.Where(st => st.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(st => st.StockTakeNumber.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(st => st.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(st => new StockTakeResponse
            {
                Id = st.Id,
                StockTakeNumber = st.StockTakeNumber,
                WarehouseId = st.WarehouseId,
                WarehouseName = st.Warehouse != null ? st.Warehouse.Name : string.Empty,
                CategoryId = st.CategoryId,
                CategoryName = st.Category != null ? st.Category.Name : string.Empty,
                Status = st.Status,
                SnapshotDate = st.SnapshotDate,
                SubmittedDate = st.SubmittedDate,
                CompletedDate = st.CompletedDate,
                CompletedBy = st.CompletedBy,
                Notes = st.Notes,
                TotalLines = st.Lines.Count,
                CountedLines = st.Lines.Count(l => l.CountedQuantity != null),
                VarianceLines = st.Lines.Count(l => l.CountedQuantity != null && l.CountedQuantity != l.ExpectedQuantity),
                TotalVarianceValue = st.Lines
                    .Where(l => l.CountedQuantity != null)
                    .Sum(l => (l.CountedQuantity!.Value - l.ExpectedQuantity) * l.UnitCost),
                CreatedBy = st.CreatedBy,
                CreatedDate = st.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return Ok(PagedResult<StockTakeResponse>.Create(items, totalCount, pageNumber, pageSize));
    }

    /// <summary>Stock take header + all lines (count sheet / variance review).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var stockTake = await _dbContext.StockTakes
            .Include(st => st.Warehouse)
            .Include(st => st.Category)
            .Include(st => st.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(st => st.Id == id && !st.Isdeleted, cancellationToken);

        if (stockTake == null)
            return NotFound(new { message = "Stock take not found" });

        var response = new StockTakeDetailResponse
        {
            Lines = stockTake.Lines
                .OrderBy(l => l.PartName).ThenBy(l => l.VariantName)
                .Select(l => new StockTakeLineResponse
                {
                    Id = l.Id,
                    PartId = l.PartId,
                    VariantId = l.VariantId,
                    PartName = l.PartName,
                    PartCode = l.PartCode,
                    VariantName = l.VariantName,
                    Location = l.Location,
                    ExpectedQuantity = l.ExpectedQuantity,
                    CountedQuantity = l.CountedQuantity,
                    Variance = l.Variance,
                    UnitCost = l.UnitCost,
                    VarianceValue = l.Variance.HasValue ? l.Variance.Value * l.UnitCost : null,
                    CountedBy = l.CountedBy,
                    CountedAt = l.CountedAt,
                    Notes = l.Notes
                }).ToList()
        };
        CopyHeader(stockTake, stockTake.Warehouse?.Name ?? string.Empty, stockTake.Category?.Name ?? string.Empty, response);

        return Ok(response);
    }

    /// <summary>
    /// Records counted quantities (base units) for one or more lines while the stock take is in
    /// COUNTING. Sending a null quantity clears a previously recorded count.
    /// </summary>
    [HttpPut("{id:guid}/counts")]
    public async Task<IActionResult> RecordCounts(Guid id, [FromBody] RecordStockTakeCountsRequest request, CancellationToken cancellationToken)
    {
        if (request.Counts == null || request.Counts.Count == 0)
            return BadRequest(new { message = "At least one count entry is required" });

        if (request.Counts.Any(c => c.CountedQuantity is < 0))
            return BadRequest(new { message = "Counted quantity cannot be negative" });

        var stockTake = await _dbContext.StockTakes
            .Include(st => st.Lines)
            .FirstOrDefaultAsync(st => st.Id == id && !st.Isdeleted, cancellationToken);

        if (stockTake == null)
            return NotFound(new { message = "Stock take not found" });

        if (stockTake.Status != "COUNTING")
            return BadRequest(new { message = $"Counts can only be recorded while the stock take is in COUNTING. Current: {stockTake.Status}" });

        var linesById = stockTake.Lines.ToDictionary(l => l.Id);
        var unknown = request.Counts.Where(c => !linesById.ContainsKey(c.LineId)).Select(c => c.LineId).ToList();
        if (unknown.Count > 0)
            return BadRequest(new { message = $"{unknown.Count} count entr{(unknown.Count == 1 ? "y" : "ies")} reference lines that do not belong to this stock take" });

        var username = _currentUserService.GetCurrentUsername();
        foreach (var entry in request.Counts)
        {
            var line = linesById[entry.LineId];
            if (entry.CountedQuantity.HasValue)
                line.RecordCount(entry.CountedQuantity.Value, username, entry.Notes);
            else
                line.ClearCount();
            line.ModifiedBy = username;
        }

        stockTake.ModifiedBy = username;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = $"{request.Counts.Count} count(s) recorded" });
    }

    /// <summary>Locks counting and moves the stock take to variance review.</summary>
    [HttpPost("{id:guid}/submit")]
    public Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(id, st => st.SubmitForReview(), cancellationToken);

    /// <summary>Reopens counting from review (e.g. a variance needs a recount).</summary>
    [HttpPost("{id:guid}/reopen")]
    public Task<IActionResult> Reopen(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(id, st => st.ReopenCounting(), cancellationToken);

    /// <summary>Cancels an open stock take. No stock is touched.</summary>
    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(id, st => st.Cancel(), cancellationToken);

    private async Task<IActionResult> TransitionAsync(Guid id, Action<StockTake> transition, CancellationToken cancellationToken)
    {
        var stockTake = await _dbContext.StockTakes
            .Include(st => st.Lines)
            .FirstOrDefaultAsync(st => st.Id == id && !st.Isdeleted, cancellationToken);

        if (stockTake == null)
            return NotFound(new { message = "Stock take not found" });

        try
        {
            transition(stockTake);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        stockTake.ModifiedBy = _currentUserService.GetCurrentUsername();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The stock take was modified by another user. Reload and try again." });
        }

        return Ok(new { id = stockTake.Id, status = stockTake.Status });
    }

    /// <summary>
    /// Applies every counted variance as a stock adjustment (level + FIFO lot sync) in ONE
    /// transaction, referencing the stock take number, then completes the stock take.
    /// The applied delta is counted − expected(snapshot), so sales made between snapshot and
    /// approval are not double-counted. Uncounted lines are skipped. If any negative variance
    /// no longer fits current stock (item sold since counting), nothing is applied and the
    /// conflicting lines are returned for recount.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var stockTake = await _dbContext.StockTakes
            .Include(st => st.Lines)
            .FirstOrDefaultAsync(st => st.Id == id && !st.Isdeleted, cancellationToken);

        if (stockTake == null)
            return NotFound(new { message = "Stock take not found" });

        if (stockTake.Status != "REVIEW")
            return BadRequest(new { message = $"Only a stock take in REVIEW can be approved. Current: {stockTake.Status}" });

        var username = _currentUserService.GetCurrentUsername();
        var response = new ApproveStockTakeResponse { Id = stockTake.Id, StockTakeNumber = stockTake.StockTakeNumber };

        try
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            IActionResult? failure = null;
            await strategy.ExecuteAsync(async () =>
            {
                // The strategy may retry this whole lambda on transient failures. Start every
                // attempt from clean state: clear tracked (possibly already-mutated) entities and
                // reload, otherwise a retry would double-apply deltas and duplicate movements.
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                var take = await _dbContext.StockTakes
                    .Include(st => st.Lines)
                    .FirstAsync(st => st.Id == id && !st.Isdeleted, cancellationToken);

                if (take.Status != "REVIEW")
                {
                    await tx.RollbackAsync(cancellationToken);
                    failure = BadRequest(new { message = $"Only a stock take in REVIEW can be approved. Current: {take.Status}" });
                    return;
                }

                var countedLines = take.Lines.Where(l => l.CountedQuantity.HasValue).ToList();
                var varianceLines = countedLines.Where(l => l.Variance != 0).ToList();

                response.AdjustmentsApplied = 0;
                response.LotSyncWarnings.Clear();
                response.LinesUnchanged = countedLines.Count - varianceLines.Count;
                response.LinesSkippedUncounted = take.Lines.Count - countedLines.Count;
                response.TotalVarianceValue = varianceLines.Sum(l => (l.Variance ?? 0) * l.UnitCost);

                // Fresh, tracked levels — stock may have moved since the snapshot.
                var levelIds = varianceLines.Select(l => l.StockLevelId).ToList();
                var levels = await _dbContext.StockLevels
                    .Where(sl => levelIds.Contains(sl.Id))
                    .ToDictionaryAsync(sl => sl.Id, cancellationToken);

                // Pre-validate every line so approval is all-or-nothing: a partial apply would
                // leave the document COMPLETED with some variances silently dropped.
                var conflicts = new List<string>();
                foreach (var line in varianceLines)
                {
                    var delta = line.Variance!.Value;
                    if (!levels.TryGetValue(line.StockLevelId, out var level) || level.Isdeleted || !level.IsActive)
                    {
                        conflicts.Add($"{LineLabel(line)}: stock level no longer exists or is inactive");
                        continue;
                    }
                    if (delta < 0 && (level.QuantityAvailableInBaseUnit < -delta || level.QuantityOnHandInBaseUnit < -delta))
                        conflicts.Add(
                            $"{LineLabel(line)}: variance {delta} no longer fits current stock (on hand {level.QuantityOnHandInBaseUnit}, reserved {level.QuantityReservedInBaseUnit}) — stock moved since counting, recount this item");
                }

                if (conflicts.Count > 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    failure = BadRequest(new
                    {
                        message = $"{conflicts.Count} line(s) can no longer be applied. Reopen counting, recount them, and approve again.",
                        conflicts
                    });
                    return;
                }

                foreach (var line in varianceLines)
                {
                    var delta = line.Variance!.Value;
                    var outcome = await _adjustmentApplier.ApplyAsync(
                        levels[line.StockLevelId],
                        delta,
                        delta,   // stock take counts are in base units
                        "COUNT_CORRECTION",
                        take.StockTakeNumber,
                        $"Stock take {take.StockTakeNumber}: expected {line.ExpectedQuantity}, counted {line.CountedQuantity}",
                        username,
                        referenceId: line.Id,
                        referenceType: "StockTakeLine",
                        cancellationToken: cancellationToken);

                    response.AdjustmentsApplied++;
                    if (outcome.LotSyncSkipped)
                        response.LotSyncWarnings.Add(
                            $"{LineLabel(line)}: stock level adjusted by {delta} but lot records could not fully mirror it (no/insufficient lot data)");
                }

                take.Complete(username);
                take.ModifiedBy = username;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            });

            if (failure != null)
                return failure;

            if (response.LotSyncWarnings.Count > 0)
                _logger.LogWarning("Stock take {Number}: {Count} line(s) applied with incomplete lot sync",
                    stockTake.StockTakeNumber, response.LotSyncWarnings.Count);

            return Ok(response);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The stock take or its stock levels were modified by another user. Reload and try again." });
        }
        catch (InvalidOperationException ex)
        {
            // Domain guard fired mid-apply (e.g. a concurrent sale between validation and apply) —
            // the transaction rolled back, nothing was applied.
            return Conflict(new { message = $"Approval aborted, no changes were applied: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving stock take {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while approving the stock take" });
        }
    }

    private static string LineLabel(StockTakeLine line) =>
        string.IsNullOrEmpty(line.VariantName) ? $"{line.PartCode} {line.PartName}".Trim() : $"{line.PartCode} {line.PartName} - {line.VariantName}".Trim();

    private static StockTakeResponse MapHeader(StockTake st, string warehouseName, string categoryName)
    {
        var response = new StockTakeResponse();
        CopyHeader(st, warehouseName, categoryName, response);
        return response;
    }

    private static void CopyHeader(StockTake st, string warehouseName, string categoryName, StockTakeResponse target)
    {
        target.Id = st.Id;
        target.StockTakeNumber = st.StockTakeNumber;
        target.WarehouseId = st.WarehouseId;
        target.WarehouseName = warehouseName;
        target.CategoryId = st.CategoryId;
        target.CategoryName = categoryName;
        target.Status = st.Status;
        target.SnapshotDate = st.SnapshotDate;
        target.SubmittedDate = st.SubmittedDate;
        target.CompletedDate = st.CompletedDate;
        target.CompletedBy = st.CompletedBy;
        target.Notes = st.Notes;
        target.TotalLines = st.Lines.Count;
        target.CountedLines = st.Lines.Count(l => l.CountedQuantity.HasValue);
        target.VarianceLines = st.Lines.Count(l => l.CountedQuantity.HasValue && l.Variance != 0);
        target.TotalVarianceValue = st.Lines.Where(l => l.CountedQuantity.HasValue).Sum(l => (l.Variance ?? 0) * l.UnitCost);
        target.CreatedBy = st.CreatedBy;
        target.CreatedDate = st.CreatedDate;
    }
}
