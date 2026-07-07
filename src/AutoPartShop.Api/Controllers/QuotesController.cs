using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.SalesOrderDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Standalone quotation endpoint for the POS / Quick Sale screen. A quote is a DRAFT sales order
/// with no invoice, payment, or stock side effects â€” it can later be turned into a sale.
/// </summary>
[Route("api/quotes")]
[Route("api/v1/quotes")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesCreate)]
public class QuotesController : ControllerBase
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(
        ISalesOrderRepository salesOrderRepository,
        IProductRepository productRepository,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ILogger<QuotesController> logger)
    {
        _salesOrderRepository = salesOrderRepository;
        _productRepository = productRepository;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuote([FromBody] QuickSaleRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.Items is null || request.Items.Count == 0)
            return BadRequest(new { message = "At least one item is required" });

        try
        {
            var quoteNumber = await _codeGenerateService.GenerateAsync("SO", cancellationToken);

            // A quote is a DRAFT sales order â€” never confirmed, so it holds no stock and raises no invoice.
            var quote = SalesOrder.Create(
                quoteNumber,
                request.CustomerId ?? Guid.Empty,
                request.CustomerName,
                request.CustomerEmail,
                request.CustomerPhone,
                null,
                request.TechnicianId,
                request.TechnicianName,
                string.Empty,
                request.Notes);

            var lineNumber = 1;
            foreach (var item in request.Items)
            {
                var part = await _productRepository.GetByIdAsync(item.PartId, cancellationToken);
                if (part is null)
                    return BadRequest(new { message = $"Part with ID {item.PartId} not found" });

                var unitPrice = item.UnitPrice > 0 ? item.UnitPrice : part.SellingPrice;
                var discountPerUnit = (unitPrice * item.Discount) / 100;

                // Quotes don't move stock, so base-unit quantity is informational â€” mirror the entered quantity.
                var line = SalesOrderLine.Create(
                    quote.Id,
                    item.PartId,
                    item.Quantity,
                    unitPrice,
                    lineNumber++,
                    unitId: item.UnitId,
                    quantityInBaseUnit: item.Quantity,
                    discount: discountPerUnit,
                    description: string.IsNullOrWhiteSpace(item.PartName) ? part.Name : item.PartName,
                    productVariantId: item.ProductVariantId);
                quote.LineItems.Add(line);
            }

            quote.CalculateTotal();
            quote.SetTax(request.VatAmount);
            quote.CreatedBy = _currentUserService.GetCurrentUsername();
            quote.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _salesOrderRepository.AddAsync(quote, cancellationToken);

            _logger.LogInformation("Quote {QuoteNumber} created for {CustomerName}", quoteNumber, request.CustomerName);

            return Ok(new { quoteId = quote.Id, quoteNumber });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the quote" });
        }
    }
}
