using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Pdf.Design;
using AutoPartShop.Api.Services;
using QuestPDF.Fluent;
using AutoPartShop.Application.DTOs.CustomerDtos;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;
[Route("api/v1/customer-account-summary")]
[ApiController]
[HasPermission(Permissions.ReportsView)]
[Produces("application/json")]
public class CustomerAccountSummaryController : ControllerBase
{
    private readonly ICustomerAccountSummaryService _summaryService;
    private readonly IShopProfileProvider _shopProfiles;
    private readonly ILogger<CustomerAccountSummaryController> _logger;

    public CustomerAccountSummaryController(
        ICustomerAccountSummaryService summaryService,
        IShopProfileProvider shopProfiles,
        ILogger<CustomerAccountSummaryController> logger)
    {
        _summaryService = summaryService;
        _shopProfiles = shopProfiles;
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

    /// <summary>
    /// Download a full Statement of Account PDF (all transactions, no pagination).
    /// </summary>
    [HttpPost("{customerId:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAccountStatementPdf(
        Guid customerId,
        [FromBody] CustomerAccountSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            query.CustomerId = customerId;
            query.PageNumber = 1;
            query.PageSize = int.MaxValue;

            var summary = await _summaryService.GetAccountSummaryAsync(query, cancellationToken);
            var shopProfile = await _shopProfiles.GetAsync(summary.CurrencySymbol, cancellationToken: cancellationToken);

            var document = new CustomerAccountStatementDocument(summary, shopProfile, DocTheme.Default with { Lang = this.GetLanguage() });
            var pdfBytes = document.GeneratePdf();

            var filename = $"account-statement-{summary.CustomerCode}-{DateTime.UtcNow:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating account statement PDF for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while generating the account statement PDF" });
        }
    }
}
