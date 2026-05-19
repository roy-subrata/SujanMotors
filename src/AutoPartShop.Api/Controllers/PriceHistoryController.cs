using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.PriceHistoryDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class PriceHistoryController : ControllerBase
{
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ILogger<PriceHistoryController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PriceHistoryController(IPriceHistoryRepository priceHistoryRepository, ICurrentUserService currentUserService, ILogger<PriceHistoryController> logger)
    {
        _priceHistoryRepository = priceHistoryRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var history = await _priceHistoryRepository.GetAllAsync(cancellationToken);
            var response = history.Select(MapToPriceHistoryResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all price history");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving price history");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (history, totalCount) = await _priceHistoryRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
            var response = history.Select(MapToPriceHistoryResponse);

            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving price history");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var history = await _priceHistoryRepository.GetByIdAsync(id, cancellationToken);
            if (history is null) return NotFound(new { message = "Price history not found" });

            return Ok(MapToPriceHistoryResponse(history));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history by ID: {PriceHistoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the price history");
        }
    }

    [HttpGet("part/{partId:guid}")]
    public async Task<IActionResult> GetByPart(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await _priceHistoryRepository.GetByPartAsync(partId, cancellationToken);
            var response = history.Select(MapToPriceHistoryResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history by part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving price history");
        }
    }

    [HttpGet("part/{partId:guid}/date-range")]
    public async Task<IActionResult> GetByPartAndDateRange(Guid partId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            if (startDate > endDate)
                return BadRequest(new { message = "StartDate must be before EndDate" });

            var history = await _priceHistoryRepository.GetByPartAndDateRangeAsync(partId, startDate, endDate, cancellationToken);
            var response = history.Select(MapToPriceHistoryResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history by date range");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving price history");
        }
    }

    [HttpGet("part/{partId:guid}/latest")]
    public async Task<IActionResult> GetLatestByPart(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await _priceHistoryRepository.GetLatestByPartAsync(partId, cancellationToken);
            if (history is null) return NotFound(new { message = "No price history found for this part" });

            return Ok(MapToPriceHistoryResponse(history));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest price history for part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving price history");
        }
    }

    [HttpGet("part/{partId:guid}/price-at-date")]
    public async Task<IActionResult> GetPriceAtDate(Guid partId, [FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        try
        {
            var price = await _priceHistoryRepository.GetPriceAtDateAsync(partId, date, cancellationToken);
            return Ok(new { partId, date, price });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price at date");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving price history");
        }
    }

    [HttpGet("reason/{reason}")]
    public async Task<IActionResult> GetByReason(string reason, CancellationToken cancellationToken)
    {
        try
        {
            var history = await _priceHistoryRepository.GetByReasonAsync(reason, cancellationToken);
            var response = history.Select(MapToPriceHistoryResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history by reason: {Reason}", reason);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving price history");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePriceHistoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PartId == Guid.Empty)
                return BadRequest(new { message = "PartId is required" });

            var history = PriceHistory.Create(
                request.PartId,
                request.OldPrice,
                request.NewPrice,
                request.EffectiveDate,
                request.Reason,
                request.ChangedBy
            );
            var currentUser = _currentUserService.GetCurrentUsername();
            history.CreatedBy = currentUser;
            history.ModifiedBy = currentUser;

            await _priceHistoryRepository.AddAsync(history, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = history.Id }, MapToPriceHistoryResponse(history));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating price history");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating price history");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _priceHistoryRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Price history not found" });

            await _priceHistoryRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting price history: {PriceHistoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting price history");
        }
    }

    private PriceHistoryResponse MapToPriceHistoryResponse(PriceHistory history)
    {
        return new PriceHistoryResponse
        {
            Id = history.Id,
            PartId = history.PartId,
            OldPrice = history.OldPrice,
            NewPrice = history.NewPrice,
            PriceDifference = history.PriceDifference,
            PercentageChange = history.PercentageChange,
            EffectiveDate = history.EffectiveDate,
            Reason = history.Reason,
            ChangedBy = history.ChangedBy,
            CreatedAt = DateTime.UtcNow
        };
    }
}
