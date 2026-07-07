using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.WarrantyDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesView)]
public class WarrantyRegistrationsController : ControllerBase
{
    private readonly IWarrantyRegistrationRepository _warrantyRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IWarrantyService _warrantyService;
    private readonly ILogger<WarrantyRegistrationsController> _logger;

    public WarrantyRegistrationsController(
        IWarrantyRegistrationRepository warrantyRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        ISalesOrderRepository salesOrderRepository,
        IWarrantyService warrantyService,
        ILogger<WarrantyRegistrationsController> logger)
    {
        _warrantyRepository = warrantyRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _salesOrderRepository = salesOrderRepository;
        _warrantyService = warrantyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var warranties = await _warrantyRepository.GetAllAsync(cancellationToken);
            var response = warranties.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranty registrations");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving warranty registrations");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warranty = await _warrantyRepository.GetByIdAsync(id, cancellationToken);
            if (warranty == null)
                return NotFound(new { message = "Warranty registration not found" });

            return Ok(MapToResponse(warranty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranty registration: {WarrantyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the warranty registration");
        }
    }

    [HttpGet("warranty-number/{warrantyNumber}")]
    public async Task<IActionResult> GetByWarrantyNumber(string warrantyNumber, CancellationToken cancellationToken)
    {
        try
        {
            var warranty = await _warrantyRepository.GetByWarrantyNumberAsync(warrantyNumber, cancellationToken);
            if (warranty == null)
                return NotFound(new { message = $"Warranty registration with number {warrantyNumber} not found" });

            return Ok(MapToResponse(warranty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranty registration by number: {WarrantyNumber}", warrantyNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the warranty registration");
        }
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomerId(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var warranties = await _warrantyRepository.GetByCustomerIdAsync(customerId, cancellationToken);
            var response = warranties.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranties for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customer warranties");
        }
    }

    [HttpGet("sales-order/{salesOrderId:guid}")]
    public async Task<IActionResult> GetBySalesOrderId(Guid salesOrderId, CancellationToken cancellationToken)
    {
        try
        {
            var warranties = await _warrantyRepository.GetBySalesOrderIdAsync(salesOrderId, cancellationToken);
            var response = warranties.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranties for sales order: {SalesOrderId}", salesOrderId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving sales order warranties");
        }
    }

    [HttpGet("part/{partId:guid}")]
    public async Task<IActionResult> GetByPartId(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var warranties = await _warrantyRepository.GetByPartIdAsync(partId, cancellationToken);
            var response = warranties.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranties for part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving part warranties");
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveWarranties(CancellationToken cancellationToken)
    {
        try
        {
            var warranties = await _warrantyRepository.GetActiveWarrantiesAsync(cancellationToken);
            var response = warranties.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active warranties");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active warranties");
        }
    }

    [HttpGet("expired")]
    public async Task<IActionResult> GetExpiredWarranties(CancellationToken cancellationToken)
    {
        try
        {
            var warranties = await _warrantyRepository.GetExpiredWarrantiesAsync(cancellationToken);
            var response = warranties.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expired warranties");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving expired warranties");
        }
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringWarranties([FromQuery] int daysFromNow = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var warranties = await _warrantyRepository.GetExpiringWarrantiesAsync(daysFromNow, cancellationToken);
            var response = warranties.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring warranties");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving expiring warranties");
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] string? status,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? partId,
        [FromQuery] DateTime? expiryDateFrom,
        [FromQuery] DateTime? expiryDateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (warranties, totalCount) = await _warrantyRepository.SearchPagedAsync(
                searchTerm, status, customerId, partId, expiryDateFrom, expiryDateTo,
                pageNumber, pageSize, cancellationToken);

            var response = warranties.Select(MapToResponse);

            return Ok(new
            {
                data = response,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching warranty registrations");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching warranty registrations");
        }
    }

    [HttpPost]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> Create(CreateWarrantyRegistrationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate part exists and has warranty
            var part = await _productRepository.GetByIdAsync(request.PartId, cancellationToken);
            if (part == null)
                return BadRequest(new { message = "Part not found" });

            if (!part.HasWarranty)
                return BadRequest(new { message = "Part does not have warranty" });

            // Validate customer exists
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return BadRequest(new { message = "Customer not found" });

            // Validate sales order exists
            var salesOrder = await _salesOrderRepository.GetByIdAsync(request.SalesOrderId, cancellationToken);
            if (salesOrder == null)
                return BadRequest(new { message = "Sales order not found" });

            // Validate customer belongs to the provided sales order
            if (salesOrder.CustomerId != request.CustomerId)
                return BadRequest(new { message = "Customer does not match the selected sales order" });

            // Validate sales order line exists on the selected sales order
            var salesOrderLine = salesOrder.LineItems.FirstOrDefault(li => li.Id == request.SalesOrderLineId);
            if (salesOrderLine == null)
                return BadRequest(new { message = "Sales order line not found for the selected sales order" });

            // Validate part belongs to the selected sales order line
            if (salesOrderLine.PartId != request.PartId)
                return BadRequest(new { message = "Part does not match the selected sales order line" });

            // Prevent duplicate warranty registration for the same sales order line
            var existingWarranties = await _warrantyRepository.GetBySalesOrderIdAsync(request.SalesOrderId, cancellationToken);
            if (existingWarranties.Any(w => w.SalesOrderLineId == request.SalesOrderLineId && w.Status != "VOID"))
                return BadRequest(new { message = "A warranty registration already exists for this sales order line" });

            // Generate warranty number
            var warrantyNumber = await _warrantyService.GenerateWarrantyNumberAsync(cancellationToken);

            // Generate certificate number
            var certificateNumber = string.IsNullOrWhiteSpace(request.CertificateNumber)
                ? $"CERT-{warrantyNumber}"
                : request.CertificateNumber;

            // Create warranty registration
            var warranty = WarrantyRegistration.Create(
                warrantyNumber: warrantyNumber,
                partId: request.PartId,
                salesOrderId: request.SalesOrderId,
                salesOrderLineId: request.SalesOrderLineId,
                customerId: request.CustomerId,
                saleDate: request.SaleDate,
                warrantyStartDate: request.WarrantyStartDate,
                warrantyPeriodMonths: request.WarrantyPeriodMonths,
                warrantyType: request.WarrantyType,
                warrantyTerms: request.WarrantyTerms,
                certificateNumber: certificateNumber
            );

            await _warrantyRepository.AddAsync(warranty, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = warranty.Id }, MapToResponse(warranty));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating warranty registration");

            var root = ex.InnerException?.Message ?? ex.Message;
            if (root.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Invalid reference data: verify customer, sales order, sales order line, and part IDs" });

            if (root.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                root.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Duplicate warranty registration detected" });

            return StatusCode(StatusCodes.Status500InternalServerError, "Database error occurred while creating warranty registration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warranty registration");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the warranty registration");
        }
    }

    [HttpPatch("{id:guid}/void")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidWarrantyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var warranty = await _warrantyRepository.GetByIdAsync(id, cancellationToken);
            if (warranty == null)
                return NotFound(new { message = "Warranty registration not found" });

            warranty.Void(request.Reason);
            await _warrantyRepository.UpdateAsync(warranty, cancellationToken);

            return Ok(MapToResponse(warranty));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding warranty registration: {WarrantyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while voiding the warranty registration");
        }
    }

    [HttpPatch("{id:guid}/check-expiry")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> CheckExpiry(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warranty = await _warrantyRepository.GetByIdAsync(id, cancellationToken);
            if (warranty == null)
                return NotFound(new { message = "Warranty registration not found" });

            warranty.CheckAndUpdateExpiry();
            await _warrantyRepository.UpdateAsync(warranty, cancellationToken);

            return Ok(MapToResponse(warranty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking warranty expiry: {WarrantyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while checking warranty expiry");
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var warranty = await _warrantyRepository.GetByIdAsync(id, cancellationToken);
            if (warranty == null)
                return NotFound(new { message = "Warranty registration not found" });

            // Fix #7: block deletion when active claims exist to prevent orphaned claim records.
            var activeStatuses = new[] { "PENDING", "UNDER_REVIEW", "APPROVED", "IN_PROGRESS" };
            var activeClaim = warranty.Claims
                .FirstOrDefault(c => activeStatuses.Contains(c.Status, StringComparer.OrdinalIgnoreCase));
            if (activeClaim != null)
                return BadRequest(new
                {
                    message = $"Cannot delete warranty {warranty.WarrantyNumber}: active claim {activeClaim.ClaimNumber} (status: {activeClaim.Status}) is attached. Close or reject the claim first."
                });

            await _warrantyRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warranty registration: {WarrantyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the warranty registration");
        }
    }

    private WarrantyRegistrationResponse MapToResponse(WarrantyRegistration warranty)
    {
        var daysUntilExpiry = (warranty.WarrantyExpiryDate - DateTime.UtcNow).Days;

        return new WarrantyRegistrationResponse
        {
            Id = warranty.Id,
            WarrantyNumber = warranty.WarrantyNumber,
            PartId = warranty.PartId,
            PartName = warranty.Part?.Name ?? "",
            PartLocalName = warranty.Part?.LocalName,
            PartSKU = warranty.Part?.SKU ?? "",
            ProductVariantId = warranty.ProductVariantId,
            VariantName = warranty.ProductVariant?.Name,
            VariantSku = warranty.ProductVariant?.SKU,
            SalesOrderId = warranty.SalesOrderId,
            SalesOrderNumber = warranty.SalesOrder?.SONumber ?? "",
            SalesOrderLineId = warranty.SalesOrderLineId,
            CustomerId = warranty.CustomerId,
            CustomerName = warranty.Customer != null ? $"{warranty.Customer.FirstName} {warranty.Customer.LastName}" : "",
            CustomerPhone = warranty.Customer?.Phone ?? "",
            SaleDate = warranty.SaleDate,
            WarrantyStartDate = warranty.WarrantyStartDate,
            WarrantyExpiryDate = warranty.WarrantyExpiryDate,
            WarrantyType = warranty.WarrantyType,
            WarrantyPeriodMonths = warranty.WarrantyPeriodMonths,
            WarrantyTerms = warranty.WarrantyTerms,
            GuaranteeMessage = BuildGuaranteeMessage(warranty),
            CertificateNumber = warranty.CertificateNumber,
            Status = warranty.Status,
            VoidReason = warranty.VoidReason,
            VoidedDate = warranty.VoidedDate,
            IsValid = warranty.IsValid(),
            DaysUntilExpiry = daysUntilExpiry,
            CreatedDate = warranty.CreatedDate,
            CreatedBy = warranty.CreatedBy,
            ModifiedDate = warranty.ModifiedDate,
            ModifiedBy = warranty.ModifiedBy
        };
    }

    private static string BuildGuaranteeMessage(WarrantyRegistration warranty)
    {
        var sourceLabel = warranty.WarrantyType switch
        {
            "MANUFACTURER" => "manufacturer",
            "SELLER" => "seller",
            "EXTENDED" => "extended",
            _ => "warranty"
        };

        var periodText = warranty.WarrantyPeriodMonths == 1
            ? "1 month"
            : $"{warranty.WarrantyPeriodMonths} months";

        return $"{periodText} {sourceLabel} warranty coverage. Resolution may be repair, replacement, or refund as per policy.";
    }
}

public class VoidWarrantyRequest
{
    public string Reason { get; set; } = string.Empty;
}
