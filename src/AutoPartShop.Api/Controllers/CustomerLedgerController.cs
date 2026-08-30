using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.LedgerDtos;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Controller for customer ledger operations.
/// Provides a unified view of all customer transactions (invoices, payments, refunds).
/// Mirrors SupplierLedgerController's shape from the customer side of the relationship.
/// </summary>
[Route("api/v1/customer-ledger")]
[ApiController]
[HasPermission(Permissions.ReportsView)]
[Produces("application/json")]
public class CustomerLedgerController : ControllerBase
{
    private readonly ICustomerLedgerService _ledgerService;
    private readonly ILogger<CustomerLedgerController> _logger;

    public CustomerLedgerController(
        ICustomerLedgerService ledgerService,
        ILogger<CustomerLedgerController> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete ledger summary for a customer including calculated balance and recent entries
    /// </summary>
    [HttpGet("{customerId:guid}/summary")]
    [ProducesResponseType(typeof(CustomerLedgerSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLedgerSummary(
        Guid customerId,
        [FromQuery] int entryLimit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _ledgerService.GetLedgerSummaryAsync(customerId, entryLimit, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger summary for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the ledger summary" });
        }
    }

    /// <summary>
    /// Get paginated ledger entries with optional filtering
    /// </summary>
    [HttpPost("{customerId:guid}/entries")]
    [ProducesResponseType(typeof(PagedCustomerLedgerResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedgerEntries(
        Guid customerId,
        [FromBody] CustomerLedgerQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            query.CustomerId = customerId;
            var result = await _ledgerService.GetLedgerEntriesAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger entries for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving ledger entries" });
        }
    }

    /// <summary>
    /// Get current calculated balance for a customer
    /// </summary>
    [HttpGet("{customerId:guid}/balance")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentBalance(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var balance = await _ledgerService.CalculateCurrentBalanceAsync(customerId, cancellationToken);
            var totalInvoiced = await _ledgerService.GetTotalInvoicedAsync(customerId, cancellationToken);
            var totalPayments = await _ledgerService.GetTotalPaymentsAsync(customerId, cancellationToken);
            var totalRefunds = await _ledgerService.GetTotalRefundsAsync(customerId, cancellationToken);
            var advanceCredit = await _ledgerService.GetAvailableAdvanceCreditAsync(customerId, cancellationToken);

            return Ok(new
            {
                customerId,
                currentBalance = balance,
                totalInvoiced,
                totalPayments,
                totalRefunds,
                availableAdvanceCredit = advanceCredit,
                calculatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating balance for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while calculating the balance" });
        }
    }

    /// <summary>
    /// Get ledger entries within a date range
    /// </summary>
    [HttpGet("{customerId:guid}/entries")]
    [ProducesResponseType(typeof(List<CustomerLedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedgerEntriesByDateRange(
        Guid customerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _ledgerService.GetLedgerEntriesAsync(customerId, fromDate, toDate, cancellationToken);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger entries for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving ledger entries" });
        }
    }
}
