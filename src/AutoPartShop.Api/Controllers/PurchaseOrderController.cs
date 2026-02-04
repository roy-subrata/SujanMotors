using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.PurchaseOrderDtos;
using AutoPartShop.Application.PurchaseOrders;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;


namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IPurchaseOrderReadRepository _purchaseOrderReadRepository;
    private readonly IGoodsReceiptRepository _goodsReceiptRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IPartRepository _partRepository;
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
        IPartRepository partRepository,
        StockManagementService stockManagementService,
        IUnitConversionService unitConversionService,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ILogger<PurchaseOrderController> logger)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _supplierRepository = supplierRepository;
        _partRepository = partRepository;
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

            var order = PurchaseOrder.Create(
                $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                request.SupplierId,
                null,  // warehouseId - optional
                request.DeliveryDate,
                request.Notes,
                request.Currency
            );

            // Set tax and discount percentages
            order.SetTaxPercentage(request.TaxPercentage);
            order.SetDiscountPercentage(request.DiscountPercentage);

            // Add line items if provided
            if (request.LineItems?.Any() == true)
            {
                int lineNumber = 1;
                foreach (var lineRequest in request.LineItems)
                {
                    // Get part to determine base unit using repository
                    var part = await _partRepository.GetByIdAsync(lineRequest.PartId, cancellationToken);

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
                            quantityInBaseUnit = await _unitConversionService.ConvertQuantityAsync(
                                lineRequest.Quantity,
                                unitId.Value,
                                part.UnitId.Value,
                                cancellationToken);
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

                    var line = PurchaseOrderLine.Create(
                        order.Id,
                        lineRequest.PartId,
                        lineRequest.Quantity,
                        lineRequest.UnitPrice,
                        lineNumber++,
                        unitId,
                        quantityInBaseUnit
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
            await _codeGenerateService.SaveGenerateCodeAsync("PO", cancellationToken);

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
                var part = await _partRepository.GetByIdAsync(lineRequest.PartId, cancellationToken);
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
                        quantityInBaseUnit = await _unitConversionService.ConvertQuantityAsync(
                            lineRequest.Quantity,
                            unitId.Value,
                            part.UnitId.Value,
                            cancellationToken);
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

                lineItemDataList.Add(new LineItemData(
                    lineRequest.Id,
                    lineRequest.PartId,
                    lineRequest.Quantity,
                    lineRequest.UnitPrice,
                    unitId,
                    quantityInBaseUnit
                ));
            }

            // Update order properties
            order.SetTaxPercentage(request.TaxPercentage);
            order.SetDiscountPercentage(request.DiscountPercentage);
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

            var grn = GoodsReceipt.Create(
                $"GRN-{DateTime.UtcNow:yyyyMMddHHmmss}",
                request.PurchaseOrderId,
                request.WarehouseId,
                request.ReceivedDate
            );
            var currentUser = _currentUserService.GetCurrentUsername();
            grn.CreatedBy = currentUser;
            grn.ModifiedBy = currentUser;

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
                    // Find matching PO line to get OrderedQuantity and PurchaseOrderLineId
                    var poLine = purchaseOrder.LineItems.FirstOrDefault(l => l.PartId == lineRequest.PartId);
                    if (poLine is null)
                        return BadRequest(new { message = $"Part {lineRequest.PartId} not found in purchase order" });

                    // Create GRN line item with cost information
                    var grnLine = GoodsReceiptLine.Create(
                        grn.Id,
                        poLine.Id,
                        lineRequest.PartId,
                        poLine.Quantity,  // OrderedQuantity from PO
                        lineRequest.ReceivedQuantity,
                        lineRequest.Condition,
                        lineRequest.UnitCost,  // Actual unit cost received
                        lineRequest.Currency,
                        lineRequest.UnitId
                    );

                    if (!string.IsNullOrEmpty(lineRequest.Notes))
                    {
                        grnLine.RejectQuantity(0, lineRequest.Notes);
                    }

                    grn.LineItems.Add(grnLine);
                }

                // Update GRN counts
                grn.UpdateCounts();
            }

            await _goodsReceiptRepository.AddAsync(grn, cancellationToken);
            await _codeGenerateService.SaveGenerateCodeAsync("GRN", cancellationToken);
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
                    // Find matching PO line to get OrderedQuantity and PurchaseOrderLineId
                    var poLine = purchaseOrder.LineItems.FirstOrDefault(l => l.PartId == lineRequest.PartId);
                    if (poLine is null)
                        return BadRequest(new { message = $"Part {lineRequest.PartId} not found in purchase order" });

                    // Create GRN line item
                    var grnLine = GoodsReceiptLine.Create(
                        grn.Id,
                        poLine.Id,
                        lineRequest.PartId,
                        poLine.Quantity,  // OrderedQuantity from PO
                        lineRequest.ReceivedQuantity,
                        lineRequest.Condition
                    );

                    if (!string.IsNullOrEmpty(lineRequest.Notes))
                    {
                        grnLine.RejectQuantity(0, lineRequest.Notes);
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

            // Update stock levels and create audit trail
            await _stockManagementService.ProcessGoodsReceiptAsync(grn, cancellationToken);

            // Update GRN status to accepted
            grn.Accept();
            grn.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _goodsReceiptRepository.UpdateAsync(grn, cancellationToken);

            // Update purchase order line received quantities and status
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(grn.PurchaseOrderId, cancellationToken);
            if (purchaseOrder != null)
            {
                // Update received quantities for each line item
                foreach (var grnLine in grn.LineItems)
                {
                    var poLine = purchaseOrder.LineItems.FirstOrDefault(l => l.PartId == grnLine.PartId);
                    if (poLine != null)
                    {
                        // Calculate total accepted quantity for this part from all accepted GRNs
                        var totalAcceptedForPart = purchaseOrder.GoodsReceipts
                            .Where(gr => gr.Status == "ACCEPTED")
                            .SelectMany(gr => gr.LineItems)
                            .Where(l => l.PartId == grnLine.PartId)
                            .Sum(l => l.AcceptedQuantity);

                        poLine.UpdateReceivedQuantity(totalAcceptedForPart);
                    }
                }

                // Update PO status (PARTIAL or DELIVERED)
                purchaseOrder.UpdateReceiptStatus();
                purchaseOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
            }

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
            GrandTotal = order.TotalAmount,
            AmountPaid = order.PaidAmount,
            OutstandingAmount = order.TotalAmount - order.PaidAmount,
            IsOverdue = DateTime.UtcNow > order.ExpectedDeliveryDate && order.Status != "DELIVERED" && order.Status != "CANCELLED",
            Notes = order.Notes,
            Lines = order.LineItems.Select(l => new PurchaseOrderLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                UnitId = l.UnitId,
                UnitName = l.Unit?.Name ?? string.Empty,
                UnitSymbol = l.Unit?.Symbol ?? string.Empty,
                Quantity = l.Quantity,
                QuantityInBaseUnit = l.QuantityInBaseUnit,
                ReceivedQuantity = l.ReceivedQuantity,
                ReceivedQuantityInBaseUnit = l.ReceivedQuantityInBaseUnit,
                UnitPrice = l.UnitPrice,
                LineTotal = l.TotalPrice
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
            DeliveryDate = grn.DeliveryDate,
            DeliveryReference = grn.DeliveryReference,
            CarrierName = grn.CarrierName,
            DriverName = grn.DriverName,
            DeliveryNotes = grn.DeliveryNotes,
            Lines = grn.LineItems.Select(l => new GoodsReceiptLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                ReceivedQuantity = l.ReceivedQuantity,
                Condition = l.Condition,
                Notes = l.Notes,
                HasDiscrepancy = l.HasDiscrepancy,
                UnitCost = l.UnitCost,
                Currency = l.Currency,
                TotalCost = l.TotalCost,
                AcceptedQuantity = l.AcceptedQuantity,
                AcceptedTotalCost = l.AcceptedTotalCost,
                UnitId = l.UnitId
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
