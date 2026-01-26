using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.DashboardDtos;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(IFinancialSummaryService financialSummaryService, ILogger<DashboardController> logger) : ControllerBase
{
    /// <summary>
    /// Get complete dashboard data including summary, trends, and top items
    /// </summary>
    [HttpPost("financial-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DashboardResponse))]
    public async Task<IActionResult> GetDashboard([FromBody] FinancialSummaryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting dashboard data for period: {Period}, {StartDate} to {EndDate}",
                request.Period, request.StartDate, request.EndDate);

            var dashboard = await financialSummaryService.GetDashboardDataAsync(request, cancellationToken);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving dashboard data" });
        }
    }

    /// <summary>
    /// Get financial summary only
    /// </summary>
    [HttpPost("summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialSummaryResponse))]
    public async Task<IActionResult> GetSummary([FromBody] FinancialSummaryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting financial summary for period: {Period}", request.Period);

            var summary = await financialSummaryService.GetFinancialSummaryAsync(request, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting financial summary");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving financial summary" });
        }
    }

    /// <summary>
    /// Get sales trend data for charts
    /// </summary>
    [HttpPost("sales-trend")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SalesTrendDto>))]
    public async Task<IActionResult> GetSalesTrend([FromBody] FinancialSummaryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting sales trend for period: {Period}", request.Period);

            var trend = await financialSummaryService.GetSalesTrendAsync(request, cancellationToken);
            return Ok(trend);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting sales trend");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving sales trend" });
        }
    }

    /// <summary>
    /// Get quick stats for today
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialSummaryResponse))]
    public async Task<IActionResult> GetTodayStats(CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.Today;
            var request = new FinancialSummaryRequest
            {
                StartDate = today,
                EndDate = today,
                Period = "DAILY"
            };

            var summary = await financialSummaryService.GetFinancialSummaryAsync(request, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting today's stats");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving today's stats" });
        }
    }

    /// <summary>
    /// Get stats for current month
    /// </summary>
    [HttpGet("month")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialSummaryResponse))]
    public async Task<IActionResult> GetMonthStats(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.Now;
            var request = new FinancialSummaryRequest
            {
                StartDate = new DateTime(now.Year, now.Month, 1),
                EndDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)),
                Period = "MONTHLY"
            };

            var summary = await financialSummaryService.GetFinancialSummaryAsync(request, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting month stats");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving month stats" });
        }
    }

    /// <summary>
    /// Get stats for current year
    /// </summary>
    [HttpGet("year")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialSummaryResponse))]
    public async Task<IActionResult> GetYearStats(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.Now;
            var request = new FinancialSummaryRequest
            {
                StartDate = new DateTime(now.Year, 1, 1),
                EndDate = new DateTime(now.Year, 12, 31),
                Period = "YEARLY"
            };

            var summary = await financialSummaryService.GetFinancialSummaryAsync(request, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting year stats");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving year stats" });
        }
    }
}
