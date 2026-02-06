using AutoPartShop.Application.Catalog;
using AutoPartShop.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CatalogController(
    ICatalogReadRepository _catalogReadRepository,
    ILogger<CatalogController> _logger) : ControllerBase
{
    [HttpGet("landing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogLandingResponse>> GetLanding(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _catalogReadRepository.GetLandingAsync(cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting catalog landing data");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving landing data");
        }
    }

    [HttpGet("categories/{categoryId:guid}/filters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogFilterResponse>> GetFilters(Guid categoryId, [FromQuery] bool includeDescendants = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _catalogReadRepository.GetFiltersAsync(categoryId, includeDescendants, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filters for category {CategoryId}", categoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving filters");
        }
    }

    [HttpPost("products/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Search([FromBody] CatalogSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var (items, total) = await _catalogReadRepository.SearchAsync(request, cancellationToken);
            return Ok(new
            {
                items,
                request.PageNumber,
                request.PageSize,
                totalCount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching catalog products");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching products");
        }
    }

    [HttpGet("products/{partId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogProductDetail>> GetProductDetail(Guid partId, CancellationToken cancellationToken = default)
    {
        try
        {
            var detail = await _catalogReadRepository.GetProductDetailAsync(partId, cancellationToken);
            if (detail == null)
                return NotFound(new { message = "Product not found" });

            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting catalog product detail for {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving product details");
        }
    }
}
