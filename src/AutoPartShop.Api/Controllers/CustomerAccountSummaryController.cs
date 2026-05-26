using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CustomerDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/customer-account-summary")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class CustomerAccountSummaryController : ControllerBase
{
    private readonly ICustomerAccountSummaryService _summaryService;
    private readonly ILogger<CustomerAccountSummaryController> _logger;

    public CustomerAccountSummaryController(
        ICustomerAccountSummaryService summaryService,
        ILogger<CustomerAccountSummaryController> logger)
    {
        _summaryService = summaryService;
        _logger = logger;
    }

    /// <summary>
    /// Get customer account summary with financial metrics and purchase item details
    /// </summary>
    [HttpPost("{customerId:guid}")]
    [ProducesResponseType(typeof(CustomerAccountSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountSummary(
        Guid customerId,
        [FromBody] CustomerAccountSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            query.CustomerId = customerId;
            var summary = await _summaryService.GetAccountSummaryAsync(query, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account summary for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the customer account summary" });
        }
    }
}
