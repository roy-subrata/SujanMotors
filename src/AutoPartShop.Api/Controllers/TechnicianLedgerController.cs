using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.LedgerDtos;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Controller for technician ledger operations.
/// Provides a unified view of all sales activity and payments for a technician.
/// </summary>
[Route("api/v1/technician-ledger")]
[ApiController]
[HasPermission(Permissions.ReportsView)]
[Produces("application/json")]
public class TechnicianLedgerController : ControllerBase
{
    private readonly ITechnicianLedgerService _ledgerService;
    private readonly ILogger<TechnicianLedgerController> _logger;

    public TechnicianLedgerController(
        ITechnicianLedgerService ledgerService,
        ILogger<TechnicianLedgerController> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete ledger summary for a technician including calculated balance and recent entries
    /// </summary>
    [HttpGet("{technicianId:guid}/summary")]
    [ProducesResponseType(typeof(TechnicianLedgerSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLedgerSummary(
        Guid technicianId,
        [FromQuery] int entryLimit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _ledgerService.GetLedgerSummaryAsync(technicianId, entryLimit, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger summary for technician: {TechnicianId}", technicianId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the ledger summary" });
        }
    }

    /// <summary>
    /// Get paginated ledger entries with optional filtering
    /// </summary>
    [HttpPost("{technicianId:guid}/entries")]
    [ProducesResponseType(typeof(PagedTechnicianLedgerResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedgerEntries(
        Guid technicianId,
        [FromBody] TechnicianLedgerQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            query.TechnicianId = technicianId;
            var result = await _ledgerService.GetLedgerEntriesAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger entries for technician: {TechnicianId}", technicianId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving ledger entries" });
        }
    }

    /// <summary>
    /// Get current calculated balance for a technician
    /// </summary>
    [HttpGet("{technicianId:guid}/balance")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentBalance(
        Guid technicianId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var balance = await _ledgerService.CalculateCurrentBalanceAsync(technicianId, cancellationToken);
            var totalSales = await _ledgerService.GetTotalSalesAsync(technicianId, cancellationToken);
            var totalPayments = await _ledgerService.GetTotalPaymentsAsync(technicianId, cancellationToken);
            var totalReturns = await _ledgerService.GetTotalReturnsAsync(technicianId, cancellationToken);

            return Ok(new
            {
                technicianId,
                currentBalance = balance,
                totalSales,
                totalPayments,
                totalReturns,
                calculatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating balance for technician: {TechnicianId}", technicianId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while calculating the balance" });
        }
    }

    /// <summary>
    /// Get ledger entries within a date range
    /// </summary>
    [HttpGet("{technicianId:guid}/entries")]
    [ProducesResponseType(typeof(List<TechnicianLedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedgerEntriesByDateRange(
        Guid technicianId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _ledgerService.GetLedgerEntriesAsync(technicianId, fromDate, toDate, cancellationToken);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger entries for technician: {TechnicianId}", technicianId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving ledger entries" });
        }
    }
}
