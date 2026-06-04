using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.PurchaseOrderDtos;
using AutoPartShop.Application.PurchaseOrders;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[Authorize]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IPurchaseOrderReadRepository _purchaseOrderReadRepository;
    private readonly IGoodsReceiptRepository _goodsReceiptRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly StockManagementService _stockManagementService;
    private readonly IUnitConversionService _unitConversionService;
    private readonly ILogger<PurchaseOrderController> _logger;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;

    public PurchaseOrderController(
        IPurchaseOrderRepository purchaseOrderRepository,
        IPurchaseOrderReadRepository purchaseOrderReadRepository,
        IGoodsReceiptRepository goodsReceiptRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        StockManagementService stockManagementService,
        IUnitConversionService unitConversionService,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ILogger<PurchaseOrderController> logger)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _stockManagementService = stockManagementService;
        _unitConversionService = unitConversionService;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _purchaseOrderReadRepository = purchaseOrderReadRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _purchaseOrderRepository.GetAllAsync(cancellationToken);
            var response = orders.Select(MapToPurchaseOrderResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all purchase orders");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase orders");
        }
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList(PurcahseQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            var (response, total) = await _purchaseOrderReadRepository.FindAllAsync(query, cancellationToken);
            return Ok(
                 PagedResult<PurchaseOrderResponse>.Create(response, total, query)
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase orders");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _purchaseOrderReadRepository.GetPurchaseOrderByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Purchase order not found" });

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase order by ID: {POId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the purchase order");
        }
    }

    [HttpGet("number/{poNumber}")]
    public async Task<IActionResult> GetByNumber(string poNumber, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _purchaseOrderRepository.GetByNumberAsync(poNumber, cancellationToken);
            if (order is null) return NotFound(new { message = "Purchase order not found" });

            return Ok(MapToPurchaseOrderResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase order by number: {PONumber}", poNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the purchase order");
        }
    }

    [HttpGet("supplier/{supplierId:guid}")]
    public async Task<IActionResult> GetBySupplier(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _purchaseOrderRepository.GetBySuppliersAsync(supplierId, cancellationToken);
            var response = orders.Select(MapToPurchaseOrderResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders by supplier: {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase orders");
        }
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _purchaseOrderRepository.GetByStatusAsync(status, cancellationToken);
            var response = orders.Select(MapToPurchaseOrderResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders by status: {Status}", status);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase orders");
        }
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _purchaseOrderRepository.GetOverdueAsync(cancellationToken);
            var response = orders.Select(MapToPurchaseOrderResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue purchase orders");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving overdue purchase orders");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.SupplierId == Guid.Empty || request.DeliveryDate == default)
                return BadRequest(new { message = "SupplierId and DeliveryDate are required" });

            var purchaseOrderNumber = await _codeGenerateService.GenerateAsync("PO", cancellationToken);
            var order = PurchaseOrder.Create(
                purchaseOrderNumber,
                request.SupplierId,
                null,  // warehouseId - optional
                request.DeliveryDate,
                request.Notes,
                request.Currency
            );

            // Set tax and discount
            order.SetTaxPercentage(request.TaxPercentage);
            order.SetDiscount(request.DiscountPercentage, request.DiscountAmount, request.DiscountType);

            // Add line items if provided
            if (request.LineItems?.Any() == true)
            {
                int lineNumber = 1;
                foreach (var lineRequest in request.LineItems)
                {
                    // Get part to determine base unit using repository
                    var part = await _productRepository.GetByIdAsync(lineRequest.PartId, cancellationToken);

                    if (part == null)
                        return BadRequest(new { message = $"Part with ID {lineRequest.PartId} not found" });

                    // Calculate quantity in base unit
                    int quantityInBaseUnit = lineRequest.Quantity;
                    Guid? unitId = lineRequest.UnitId;

                    // If unitId is provided and part has no base unit, just use the provided unit
                    if (unitId.HasValue && !part.UnitId.HasValue)
                    {
                        // Part has no base unit, use provided unit and quantity as-is
                        quantityInBaseUnit = lineRequest.Quantity;
                    }
                    // If unitId is provided and different from part's base unit, convert
                    else if (unitId.HasValue && part.UnitId.HasValue && unitId.Value != part.UnitId.Value)
                    {
                        try
                        {
                            var qtyInBaseUnit = await _unitConversionService.ConvertQuantityAsync(
                                lineRequest.Quantity,
                                unitId.Value,
                                part.UnitId.Value);
                            quantityInBaseUnit = (int)Math.Round(qtyInBaseUnit);
                        }
                        catch (InvalidOperationException ex)
                        {
                            return BadRequest(new { message = $"Unit conversion not configured: {ex.Message}" });
                        }
                    }
                    else if (!unitId.HasValue)
                    {
                        // If no unit specified, use part's base unit
                        unitId = part.UnitId;
                    }

                    // Enforce variant selection when product has active variants
                    var hasVariants = await _productRepository.HasActiveVariantsAsync(lineRequest.PartId, cancellationToken);
                    if (hasVariants && !lineRequest.VariantId.HasValue)
                        return BadRequest(new { message = $"Product '{part.Name}' has variants — please select a specific variant" });

                    var line = PurchaseOrderLine.Create(
                        order.Id,
                        lineRequest.PartId,
                        lineRequest.Quantity,
                        lineRequest.UnitPrice,
                        lineNumber++,
                        unitId,
                        quantityInBaseUnit,
                        variantId: lineRequest.VariantId
                    );
                    order.LineItems.Add(line);
                }
            }

            // Calculate totals based on line items and tax/discount
            order.CalculateTotal();

            var currentUser = _currentUserService.GetCurrentUsername();
            order.CreatedBy = currentUser;
            order.ModifiedBy = currentUser;

            await _purchaseOrderRepository.AddAsync(order, cancellationToken);

            // Reload the order with navigation properties using repository
            var createdOrder = await _purchaseOrderReadRepository.GetPurchaseOrderByIdAsync(order.Id, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, createdOrder);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the purchase order");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Get existing order with line items
            var order = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null)
                return NotFound(new { message = "Purchase order not found" });

            // Prepare line item data with unit conversion (application concern)
            var lineItemDataList = new List<LineItemData>();
            foreach (var lineRequest in request.LineItems)
            {
                // Get part to determine base unit using repository
                var part = await _productRepository.GetByIdAsync(lineRequest.PartId, cancellationToken);
                if (part == null)
                    return BadRequest(new { message = $"Part with ID {lineRequest.PartId} not found" });

                // Calculate quantity in base unit
                int quantityInBaseUnit = lineRequest.Quantity;
                Guid? unitId = lineRequest.UnitId;

                // Handle unit conversion
                if (unitId.HasValue && !part.UnitId.HasValue)
                {
                    quantityInBaseUnit = lineRequest.Quantity;
                }
                else if (unitId.HasValue && part.UnitId.HasValue && unitId.Value != part.UnitId.Value)
                {
                    try
                    {
                        var qtyInBaseUnit = await _unitConversionService.ConvertQuantityAsync(
                            lineRequest.Quantity,
                            unitId.Value,
                            part.UnitId.Value);
                        quantityInBaseUnit = (int)Math.Round(qtyInBaseUnit);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(new { message = $"Unit conversion not configured: {ex.Message}" });
                    }
                }
                else if (!unitId.HasValue)
                {
                    unitId = part.UnitId;
                }

                var hasVariants = await _productRepository.HasActiveVariantsAsync(lineRequest.PartId, cancellationToken);
                if (hasVariants && !lineRequest.VariantId.HasValue)
                    return BadRequest(new { message = $"Product '{part.Name}' has variants — please select a specific variant" });

                lineItemDataList.Add(new LineItemData(
                    lineRequest.Id,
                    lineRequest.PartId,
                    lineRequest.VariantId,
                    lineRequest.Quantity,
                    lineRequest.UnitPrice,
                    unitId,
                    quantityInBaseUnit
                ));
            }

            // Update order properties
            order.SetTaxPercentage(request.TaxPercentage);
            order.SetDiscount(request.DiscountPercentage, request.DiscountAmount, request.DiscountType);
            order.UpdateNotes(request.Notes);
            order.UpdateExpectedDeliveryDate(request.DeliveryDate);

            // Domain handles line item sync (business logic)
            order.SyncLineItems(lineItemDataList);

            order.ModifiedBy = _currentUserService.GetCurrentUsername();

            // Repository handles persistence (EF change tracking does the rest)
            await _purchaseOrderRepository.UpdateAsync(order, cancellationToken);

            // Reload for response
            var updatedOrder = await _purchaseOrderReadRepository.GetPurchaseOrderByIdAsync(order.Id, cancellationToken);

            return Ok(updatedOrder);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order: {POId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the purchase order");
        }
    }

    [HttpPatch("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Purchase order not found" });

            order.Submit();
            order.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _purchaseOrderRepository.UpdateAsync(order, cancellationToken);

            return Ok(MapToPurchaseOrderResponse(order));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting purchase order: {POId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while submitting the purchase order");
        }
    }

    [HttpPatch("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Purchase order not found" });

            var currentUser = _currentUserService.GetCurrentUsername();
            order.Confirm(currentUser);  // Approver
            order.ModifiedBy = currentUser;
            await _purchaseOrderRepository.UpdateAsync(order, cancellationToken);

            // NOTE: Supplier balance is NOT updated here.
            // Balance is now calculated from transactions via SupplierLedgerService.
            // The confirmed PO automatically contributes to the calculated balance.
            _logger.LogInformation("Purchase order {PONumber} confirmed. Amount: {Amount}. Balance calculated from transactions.",
                order.PONumber, order.TotalAmount);

            return Ok(MapToPurchaseOrderResponse(order));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming purchase order: {POId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while confirming the purchase order");
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Purchase order not found" });

            // Track if order was confirmed before cancellation (for balance reversal)
            bool wasConfirmed = order.Status == "CONFIRMED";
            decimal orderTotal = order.TotalAmount;
            Guid supplierId = order.SupplierId;

            var currentUser = _currentUserService.GetCurrentUsername();
            order.Cancel();
            order.ModifiedBy = currentUser;
            await _purchaseOrderRepository.UpdateAsync(order, cancellationToken);

            // IMPORTANT: Reverse supplier balance if order was confirmed
            // NOTE: Supplier balance is NOT updated here.
            // Balance is now calculated from transactions via SupplierLedgerService.
            // Cancelled POs are excluded from the calculated balance.
            if (wasConfirmed)
            {
                _logger.LogInformation("Purchase order {PONumber} cancelled. Amount: {Amount}. Balance calculated from transactions.",
                    order.PONumber, orderTotal);
            }

            return Ok(MapToPurchaseOrderResponse(order));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling purchase order: {POId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while cancelling the purchase order");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _purchaseOrderRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Purchase order not found" });

            await _purchaseOrderRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting purchase order: {POId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the purchase order");
        }
    }

    // GRN endpoints
    [HttpPost("grn")]
    public async Task<IActionResult> CreateGRN(CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PurchaseOrderId == Guid.Empty || request.WarehouseId == Guid.Empty)
                return BadRequest(new { message = "PurchaseOrderId and WarehouseId are required" });

            // Get the purchase order to retrieve line items and their details
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId, cancellationToken);
            if (purchaseOrder is null)
                return NotFound(new { message = "Purchase order not found" });

            var grnNumber = await _codeGenerateService.GenerateAsync("GRN", cancellationToken);
            var grn = GoodsReceipt.Create(
                grnNumber,
                request.PurchaseOrderId,
                request.WarehouseId,
                request.ReceivedDate
            );
            var currentUser = _currentUserService.GetCurrentUsername();
            grn.CreatedBy = currentUser;
            grn.ModifiedBy = currentUser;

            // Set supplier invoice information
            grn.SetInvoiceInformation(
                request.SupplierInvoiceNumber ?? string.Empty,
                request.SupplierInvoiceDate,
                request.InvoiceNotProvided);

            // Set delivery information if provided
            if (request.DeliveryDate.HasValue || !string.IsNullOrEmpty(request.DeliveryReference))
            {
                grn.SetDeliveryInformation(
                    request.DeliveryDate,
                    request.DeliveryReference,
                    request.CarrierName,
                    request.DriverName,
                    request.DeliveryNotes
                );
            }

            // Process line items from request
            if (request.Lines?.Any() == true)
            {
                foreach (var lineRequest in request.Lines)
                {
                    // Find matching PO line to get OrderedQuantity and PurchaseOrderLineId.
                    // Match by PurchaseOrderLineId when provided so multiple lines of the same part
                    // (e.g. different variants) are disambiguated; fall back to PartId for older clients.
                    var poLine = (lineRequest.PurchaseOrderLineId is Guid polId && polId != Guid.Empty
                            ? purchaseOrder.LineItems.FirstOrDefault(l => l.Id == polId)
                            : null)
                        ?? purchaseOrder.LineItems.FirstOrDefault(l => l.PartId == lineRequest.PartId);
                    if (poLine is null)
                        return BadRequest(new { message = $"Part {lineRequest.PartId} not found in purchase order" });

                    // Calculate remaining quantity considering already accepted/verified GRNs for THIS PO line
                    var alreadyReceived = purchaseOrder.GoodsReceipts
                        .Where(gr => gr.Status == "ACCEPTED" || gr.Status == "VERIFIED")
                        .SelectMany(gr => gr.LineItems)
                        .Where(grl => grl.PurchaseOrderLineId == poLine.Id)
                        .Sum(grl => grl.ReceivedQuantity);

                    var remainingQty = poLine.Quantity - alreadyReceived;

                    if (lineRequest.ReceivedQuantity > remainingQty)
                    {
                        return BadRequest(new {
                            message = $"Received quantity ({lineRequest.ReceivedQuantity}) exceeds remaining ordered quantity ({remainingQty}) for part {lineRequest.PartId}. Already received: {alreadyReceived}, Ordered: {poLine.Quantity}"
                        });
                    }

                    // Get the Part to calculate base unit quantities
                    var part = await _productRepository.GetByIdAsync(lineRequest.PartId, cancellationToken);

                    if (part == null)
                        return BadRequest(new { message = $"Part {lineRequest.PartId} not found" });

                    // Calculate base unit quantities if unit conversion is needed
                    int orderedQuantityInBaseUnit = poLine.Quantity;
                    int receivedQuantityInBaseUnit = lineRequest.ReceivedQuantity;
                    decimal unitCostInBaseUnit = lineRequest.UnitCost;

                    // Only convert if part has a base unit AND received unit differs from base unit.
                    // Fail fast if the conversion is required but not configured — stock is tracked
                    // in base units, so silently using display quantities would corrupt on-hand stock.
                    if (part.BaseUnitId.HasValue && lineRequest.UnitId.HasValue && lineRequest.UnitId.Value != part.BaseUnitId.Value)
                    {
                        decimal conversionFactor;
                        try
                        {
                            conversionFactor = await _unitConversionService.GetConversionFactorAsync(
                                lineRequest.UnitId.Value,
                                part.BaseUnitId.Value);
                        }
                        catch (Exception ex)
                        {
                            return BadRequest(new { message = $"Unit conversion not configured for part '{part.Name}': {ex.Message}" });
                        }

                        if (conversionFactor <= 0)
                            return BadRequest(new { message = $"Unit conversion not configured for part '{part.Name}'." });

                        orderedQuantityInBaseUnit = (int)Math.Round(poLine.Quantity * conversionFactor, MidpointRounding.AwayFromZero);
                        receivedQuantityInBaseUnit = (int)Math.Round(lineRequest.ReceivedQuantity * conversionFactor, MidpointRounding.AwayFromZero);
                        unitCostInBaseUnit = lineRequest.UnitCost / conversionFactor;
                    }

                    // Create GRN line item with cost information and base unit quantities
                    var grnLine = GoodsReceiptLine.Create(
                        grn.Id,
                        poLine.Id,
                        lineRequest.PartId,
                        poLine.Quantity,  // OrderedQuantity from PO
                        lineRequest.ReceivedQuantity,
                        lineRequest.Condition,
                        lineRequest.UnitCost,  // Actual unit cost received
                        lineRequest.Currency,
                        lineRequest.UnitId,
                        orderedQuantityInBaseUnit,
                        receivedQuantityInBaseUnit,
                        0, // rejectedQuantityInBaseUnit
                        unitCostInBaseUnit,
                        lineRequest.SellingPrice,
                        lineRequest.HasWarranty,
                        lineRequest.WarrantyPeriodMonths,
                        lineRequest.WarrantyType,
                        lineRequest.WarrantyTerms,
                        lineRequest.BatchNumber,
                        lineRequest.ExpiryDate,
                        poLine.VariantId  // carry variant from the matched PO line (SKU-level stock)
                    );

                    if (!string.IsNullOrEmpty(lineRequest.Notes))
                    {
                        grnLine.SetNotes(lineRequest.Notes);
                    }

                    grn.LineItems.Add(grnLine);
                }

                // Update GRN counts
                grn.UpdateCounts();
            }

            await _goodsReceiptRepository.AddAsync(grn, cancellationToken);
            return CreatedAtAction(nameof(GetGRNById), new { id = grn.Id }, MapToGoodsReceiptResponse(grn));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating goods receipt");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the goods receipt");
        }
    }

    [HttpGet("grn/{id:guid}")]
    public async Task<IActionResult> GetGRNById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _goodsReceiptRepository.GetByIdAsync(id, cancellationToken);
            if (grn is null) return NotFound(new { message = "Goods receipt not found" });

            return Ok(MapToGoodsReceiptResponse(grn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goods receipt by ID: {GRNId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the goods receipt");
        }
    }

    [HttpGet("grn/list")]
    public async Task<IActionResult> GetGRNList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var allGRNs = await _goodsReceiptRepository.GetAllAsync(cancellationToken);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                allGRNs = allGRNs.Where(g =>
                    g.GRNNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    g.PurchaseOrderId.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                );
            }

            var grnList = allGRNs.ToList();
            var totalCount = grnList.Count;
            var grns = grnList
                .OrderByDescending(g => g.ReceiptDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new
            {
                data = grns.Select(MapToGoodsReceiptResponse).ToList(),
                pagination = new
                {
                    pageNumber,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goods receipts list");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving goods receipts" });
        }
    }

    [HttpGet("grn/number/{grnNumber}")]
    public async Task<IActionResult> GetGRNByNumber(string grnNumber, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _goodsReceiptRepository.GetByNumberAsync(grnNumber, cancellationToken);
            if (grn is null) return NotFound(new { message = "Goods receipt not found" });

            return Ok(MapToGoodsReceiptResponse(grn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goods receipt by number: {GRNNumber}", grnNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the goods receipt");
        }
    }

    [HttpPut("grn/{id:guid}")]
    public async Task<IActionResult> UpdateGRN(Guid id, CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _goodsReceiptRepository.GetByIdAsync(id, cancellationToken);
            if (grn is null) return NotFound(new { message = "Goods receipt not found" });

            // Only allow updates if GRN is still pending
            if (grn.Status != "PENDING")
                return BadRequest(new { message = "Only pending goods receipts can be edited" });

            // Get the purchase order to retrieve line items and their details
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId, cancellationToken);
            if (purchaseOrder is null)
                return NotFound(new { message = "Purchase order not found" });

            // Update basic GRN info
            grn.ModifiedBy = _currentUserService.GetCurrentUsername();

            // Clear and repopulate line items
            grn.LineItems.Clear();

            if (request.Lines?.Any() == true)
            {
                foreach (var lineRequest in request.Lines)
                {
                    // Find matching PO line to get OrderedQuantity and PurchaseOrderLineId.
                    // Match by PurchaseOrderLineId when provided so multiple lines of the same part
                    // (e.g. different variants) are disambiguated; fall back to PartId for older clients.
                    var poLine = (lineRequest.PurchaseOrderLineId is Guid polId && polId != Guid.Empty
                            ? purchaseOrder.LineItems.FirstOrDefault(l => l.Id == polId)
                            : null)
                        ?? purchaseOrder.LineItems.FirstOrDefault(l => l.PartId == lineRequest.PartId);
                    if (poLine is null)
                        return BadRequest(new { message = $"Part {lineRequest.PartId} not found in purchase order" });

                    // Calculate remaining quantity considering already accepted/verified GRNs for THIS PO line (excluding current GRN being updated)
                    var alreadyReceived = purchaseOrder.GoodsReceipts
                        .Where(gr => (gr.Status == "ACCEPTED" || gr.Status == "VERIFIED") && gr.Id != id)
                        .SelectMany(gr => gr.LineItems)
                        .Where(grl => grl.PurchaseOrderLineId == poLine.Id)
                        .Sum(grl => grl.ReceivedQuantity);

                    var remainingQty = poLine.Quantity - alreadyReceived;

                    if (lineRequest.ReceivedQuantity > remainingQty)
                    {
                        return BadRequest(new {
                            message = $"Received quantity ({lineRequest.ReceivedQuantity}) exceeds remaining ordered quantity ({remainingQty}) for part {lineRequest.PartId}. Already received: {alreadyReceived}, Ordered: {poLine.Quantity}"
                        });
                    }

                    // Get the Part to calculate base unit quantities
                    var part = await _productRepository.GetByIdAsync(lineRequest.PartId, cancellationToken);

                    if (part == null)
                        return BadRequest(new { message = $"Part {lineRequest.PartId} not found" });

                    // Calculate base unit quantities if unit conversion is needed
                    int orderedQuantityInBaseUnit = poLine.Quantity;
                    int receivedQuantityInBaseUnit = lineRequest.ReceivedQuantity;
                    decimal unitCostInBaseUnit = lineRequest.UnitCost;

                    // Only convert if part has a base unit AND received unit differs from base unit.
                    // Fail fast if the conversion is required but not configured — stock is tracked
                    // in base units, so silently using display quantities would corrupt on-hand stock.
                    if (part.BaseUnitId.HasValue && lineRequest.UnitId.HasValue && lineRequest.UnitId.Value != part.BaseUnitId.Value)
                    {
                        decimal conversionFactor;
                        try
                        {
                            conversionFactor = await _unitConversionService.GetConversionFactorAsync(
                                lineRequest.UnitId.Value,
                                part.BaseUnitId.Value);
                        }
                        catch (Exception ex)
                        {
                            return BadRequest(new { message = $"Unit conversion not configured for part '{part.Name}': {ex.Message}" });
                        }

                        if (conversionFactor <= 0)
                            return BadRequest(new { message = $"Unit conversion not configured for part '{part.Name}'." });

                        orderedQuantityInBaseUnit = (int)Math.Round(poLine.Quantity * conversionFactor, MidpointRounding.AwayFromZero);
                        receivedQuantityInBaseUnit = (int)Math.Round(lineRequest.ReceivedQuantity * conversionFactor, MidpointRounding.AwayFromZero);
                        unitCostInBaseUnit = lineRequest.UnitCost / conversionFactor;
                    }

                    // Create GRN line item with cost information and base unit quantities
                    var grnLine = GoodsReceiptLine.Create(
                        grn.Id,
                        poLine.Id,
                        lineRequest.PartId,
                        poLine.Quantity,  // OrderedQuantity from PO
                        lineRequest.ReceivedQuantity,
                        lineRequest.Condition,
                        lineRequest.UnitCost,  // Actual unit cost received
                        lineRequest.Currency,
                        lineRequest.UnitId,
                        orderedQuantityInBaseUnit,
                        receivedQuantityInBaseUnit,
                        0, // rejectedQuantityInBaseUnit
                        unitCostInBaseUnit,
                        lineRequest.SellingPrice,
                        lineRequest.HasWarranty,
                        lineRequest.WarrantyPeriodMonths,
                        lineRequest.WarrantyType,
                        lineRequest.WarrantyTerms,
                        lineRequest.BatchNumber,
                        lineRequest.ExpiryDate,
                        poLine.VariantId  // carry variant from the matched PO line (SKU-level stock)
                    );

                    if (!string.IsNullOrEmpty(lineRequest.Notes))
                    {
                        grnLine.SetNotes(lineRequest.Notes);
                    }

                    grn.LineItems.Add(grnLine);
                }

                // Update GRN counts
                grn.UpdateCounts();
            }

            await _goodsReceiptRepository.UpdateAsync(grn, cancellationToken);

            return Ok(MapToGoodsReceiptResponse(grn));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating goods receipt: {GRNId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the goods receipt");
        }
    }

    [HttpPatch("grn/{id:guid}/verify")]
    public async Task<IActionResult> VerifyGRN(Guid id, [FromQuery] string verifiedBy, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _goodsReceiptRepository.GetByIdAsync(id, cancellationToken);
            if (grn is null) return NotFound(new { message = "Goods receipt not found" });

            grn.Verify(verifiedBy);
            grn.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _goodsReceiptRepository.UpdateAsync(grn, cancellationToken);

            return Ok(MapToGoodsReceiptResponse(grn));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying goods receipt: {GRNId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while verifying the goods receipt");
        }
    }

    [HttpPatch("grn/{id:guid}/accept")]
    public async Task<IActionResult> AcceptGRN(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _goodsReceiptRepository.GetByIdAsync(id, cancellationToken);
            if (grn is null) return NotFound(new { message = "Goods receipt not found" });

            if (grn.Status != "VERIFIED")
                return BadRequest(new { message = "Only verified goods receipts can be accepted" });

            // Prevent duplicate processing - check if already accepted
            if (grn.Status == "ACCEPTED")
                return BadRequest(new { message = "This goods receipt has already been accepted" });

            // Update stock levels and update PO receipt status (handled inside ProcessGoodsReceiptAsync)
            await _stockManagementService.ProcessGoodsReceiptAsync(grn, cancellationToken);

            // Update GRN status to accepted
            grn.Accept();
            grn.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _goodsReceiptRepository.UpdateAsync(grn, cancellationToken);

            return Ok(MapToGoodsReceiptResponse(grn));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting goods receipt: {GRNId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while accepting the goods receipt");
        }
    }

    [HttpPatch("grn/{id:guid}/update-pricing")]
    public async Task<IActionResult> UpdateGRNPricing(Guid id, [FromBody] UpdateGRNPricingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _goodsReceiptRepository.GetByIdAsync(id, cancellationToken);
            if (grn is null) return NotFound(new { message = "Goods receipt not found" });

            if (grn.Status != "VERIFIED")
                return BadRequest(new { message = "Pricing can only be set on verified goods receipts" });

            foreach (var linePricing in request.Lines)
            {
                var line = grn.LineItems.FirstOrDefault(l => l.Id == linePricing.LineId);
                if (line is not null)
                {
                    line.UpdatePricing(
                        linePricing.SellingPrice,
                        linePricing.HasWarranty,
                        linePricing.WarrantyPeriodMonths,
                        linePricing.WarrantyType,
                        linePricing.WarrantyTerms
                    );
                }
            }

            grn.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _goodsReceiptRepository.UpdateAsync(grn, cancellationToken);

            return Ok(MapToGoodsReceiptResponse(grn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pricing for goods receipt: {GRNId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating pricing");
        }
    }

    [HttpPatch("grn/{id:guid}/reject")]
    public async Task<IActionResult> RejectGRN(Guid id, [FromQuery] string reason = "", CancellationToken cancellationToken = default)
    {
        try
        {
            var grn = await _goodsReceiptRepository.GetByIdAsync(id, cancellationToken);
            if (grn is null) return NotFound(new { message = "Goods receipt not found" });

            grn.Reject(reason);
            grn.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _goodsReceiptRepository.UpdateAsync(grn, cancellationToken);

            return Ok(MapToGoodsReceiptResponse(grn));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting goods receipt: {GRNId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while rejecting the goods receipt");
        }
    }

    private PurchaseOrderResponse MapToPurchaseOrderResponse(PurchaseOrder order)
    {
        return new PurchaseOrderResponse
        {
            Id = order.Id,
            PONumber = order.PONumber,
            SupplierId = order.SupplierId,
            SupplierName = order.Supplier is not null ? order.Supplier.Name : string.Empty,
            SupplierCode = order.Supplier is not null ? order.Supplier.Code : string.Empty,
            OrderDate = order.PODate,
            DeliveryDate = order.ExpectedDeliveryDate,
            Status = order.Status,
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            TaxPercentage = order.TaxPercentage,
            Discount = order.DiscountAmount,
            DiscountPercentage = order.DiscountPercentage,
            DiscountAmount = order.DiscountFixedAmount,
            DiscountType = order.DiscountType,
            GrandTotal = order.TotalAmount,
            AmountPaid = order.PaidAmount,
            OutstandingAmount = order.TotalAmount - order.PaidAmount,
            IsOverdue = DateTime.UtcNow > order.ExpectedDeliveryDate && order.Status != "DELIVERED" && order.Status != "CANCELLED",
            Notes = order.Notes,
            Lines = order.LineItems.Select(l => new PurchaseOrderLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                PartName = l.Part?.Name ?? string.Empty,
                VariantId = l.VariantId,
                VariantName = l.Variant?.Name,
                VariantCode = l.Variant?.Code,
                DisplayName = VariantNaming.Compose(l.Part?.Name, l.Variant?.Name),
                PartBaseUnitId = l.Part?.UnitId,
                UnitId = l.UnitId,
                UnitName = l.Unit?.Name ?? string.Empty,
                UnitSymbol = l.Unit?.Symbol ?? string.Empty,
                Quantity = l.Quantity,
                QuantityInBaseUnit = l.QuantityInBaseUnit,
                ReceivedQuantity = l.ReceivedQuantity,
                ReceivedQuantityInBaseUnit = l.ReceivedQuantityInBaseUnit,
                UnitPrice = l.UnitPrice,
                LineTotal = l.TotalPrice,
                PartDefaultSellingPrice = l.Part?.SellingPrice ?? 0,
                PartMinMarginPercent = 0m
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
    }

    private GoodsReceiptResponse MapToGoodsReceiptResponse(GoodsReceipt grn)
    {
        return new GoodsReceiptResponse
        {
            Id = grn.Id,
            GRNNumber = grn.GRNNumber,
            PurchaseOrderId = grn.PurchaseOrderId,
            PONumber = grn.PurchaseOrder?.PONumber ?? string.Empty,
            WarehouseId = grn.WarehouseId,
            WarehouseName = grn.Warehouse?.Name ?? string.Empty,
            ReceivedDate = grn.ReceiptDate,
            Status = grn.Status,
            TotalItemsReceived = grn.TotalItemsReceived,
            DiscrepancyCount = grn.DiscrepancyCount,
            VerifiedBy = grn.VerifiedBy,
            VerificationDate = grn.VerificationDate,
            SupplierInvoiceNumber = grn.SupplierInvoiceNumber,
            SupplierInvoiceDate = grn.SupplierInvoiceDate,
            InvoiceNotProvided = grn.InvoiceNotProvided,
            DeliveryDate = grn.DeliveryDate,
            DeliveryReference = grn.DeliveryReference,
            CarrierName = grn.CarrierName,
            DriverName = grn.DriverName,
            DeliveryNotes = grn.DeliveryNotes,
            Lines = grn.LineItems.Select(l => new GoodsReceiptLineResponse
            {
                Id = l.Id,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                PartId = l.PartId,
                PartName = l.Part?.Name ?? string.Empty,
                PartSKU = l.Part?.SKU ?? string.Empty,
                OrderedQuantity = l.OrderedQuantity,
                ReceivedQuantity = l.ReceivedQuantity,
                RejectedQuantity = l.RejectedQuantity,
                AcceptedQuantity = l.AcceptedQuantity,
                Condition = l.Condition,
                Notes = l.Notes,
                HasDiscrepancy = l.HasDiscrepancy,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate,
                UnitCost = l.UnitCost,
                Currency = l.Currency,
                UnitId = l.UnitId,
                TotalCost = l.TotalCost,
                AcceptedTotalCost = l.AcceptedTotalCost,
                SellingPrice = l.SellingPrice,
                HasWarranty = l.HasWarranty,
                WarrantyPeriodMonths = l.WarrantyPeriodMonths,
                WarrantyType = l.WarrantyType,
                WarrantyTerms = l.WarrantyTerms
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
