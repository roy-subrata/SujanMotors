using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.DashboardDtos;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[HasPermission(Permissions.ReportsView)]
public class DashboardController(
    IFinancialSummaryService financialSummaryService,
    IShopClock shopClock,
    ILogger<DashboardController> logger) : ControllerBase
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
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
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
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
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
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
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
            // The shop's calendar day, not the server's. On a UTC host these differ by the
            // whole offset, which would start "today" at 06:00 local for a UTC+6 shop.
            var today = shopClock.Today.ToDateTime(TimeOnly.MinValue);
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
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
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
            // Month boundaries follow the shop's calendar, so the month rolls over at local
            // midnight rather than 06:00 local on a UTC host.
            var now = shopClock.Now;
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
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
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
            // Year boundaries follow the shop's calendar (see GetMonthStats).
            var now = shopClock.Now;
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
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }
}
