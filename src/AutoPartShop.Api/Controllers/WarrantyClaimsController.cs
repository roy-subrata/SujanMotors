using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.WarrantyDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class WarrantyClaimsController : ControllerBase
{
    private const string WarrantyReplacementOutReason = "WARRANTY_REPLACEMENT_OUT";
    private const string WarrantyDefectiveReturnReason = "WARRANTY_DEFECTIVE_RETURN";
    private const string WarrantyRefundReturnReason = "WARRANTY_REFUND_RETURN";
    private const string WarrantyDefectiveSentToVendorReason = "WARRANTY_DEFECTIVE_SENT_TO_VENDOR";
    private const string WarrantyReplacementReceivedFromVendorReason = "WARRANTY_REPLACEMENT_RECEIVED_FROM_VENDOR";

    private readonly IWarrantyClaimRepository _claimRepository;
    private readonly IWarrantyRegistrationRepository _warrantyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ITechnicianRepository _technicianRepository;
    private readonly IWarrantyService _warrantyService;
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly ISalesReturnRepository _salesReturnRepository;
    private readonly ICustomerPaymentRepository _customerPaymentRepository;
    private readonly ICustomerCreditNoteRepository _customerCreditNoteRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<WarrantyClaimsController> _logger;

    public WarrantyClaimsController(
        IWarrantyClaimRepository claimRepository,
        IWarrantyRegistrationRepository warrantyRepository,
        ICustomerRepository customerRepository,
        ITechnicianRepository technicianRepository,
        IWarrantyService warrantyService,
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        ISalesOrderRepository salesOrderRepository,
        ISalesReturnRepository salesReturnRepository,
        ICustomerPaymentRepository customerPaymentRepository,
        ICustomerCreditNoteRepository customerCreditNoteRepository,
        ICurrentUserService currentUserService,
        AutoPartDbContext dbContext,
        ILogger<WarrantyClaimsController> logger)
    {
        _claimRepository = claimRepository;
        _warrantyRepository = warrantyRepository;
        _customerRepository = customerRepository;
        _technicianRepository = technicianRepository;
        _warrantyService = warrantyService;
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _salesOrderRepository = salesOrderRepository;
        _salesReturnRepository = salesReturnRepository;
        _customerPaymentRepository = customerPaymentRepository;
        _customerCreditNoteRepository = customerCreditNoteRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetAllAsync(cancellationToken);
            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranty claims");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving warranty claims");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranty claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the warranty claim");
        }
    }

    [HttpGet("claim-number/{claimNumber}")]
    public async Task<IActionResult> GetByClaimNumber(string claimNumber, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByClaimNumberAsync(claimNumber, cancellationToken);
            if (claim == null)
                return NotFound(new { message = $"Warranty claim with number {claimNumber} not found" });

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warranty claim by number: {ClaimNumber}", claimNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the warranty claim");
        }
    }

    [HttpGet("warranty/{warrantyRegistrationId:guid}")]
    public async Task<IActionResult> GetByWarrantyRegistrationId(Guid warrantyRegistrationId, CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetByWarrantyRegistrationIdAsync(warrantyRegistrationId, cancellationToken);
            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claims for warranty: {WarrantyId}", warrantyRegistrationId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving warranty claims");
        }
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomerId(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetByCustomerIdAsync(customerId, cancellationToken);
            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claims for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customer claims");
        }
    }

    [HttpGet("technician/{technicianId:guid}")]
    public async Task<IActionResult> GetByTechnicianId(Guid technicianId, CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetByTechnicianIdAsync(technicianId, cancellationToken);
            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claims for technician: {TechnicianId}", technicianId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving technician claims");
        }
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetByStatusAsync(status, cancellationToken);
            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claims by status: {Status}", status);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving claims");
        }
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingClaims(CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetPendingClaimsAsync(cancellationToken);
            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending claims");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving pending claims");
        }
    }

    [HttpGet("in-progress")]
    public async Task<IActionResult> GetInProgressClaims(CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetInProgressClaimsAsync(cancellationToken);
            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving in-progress claims");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving in-progress claims");
        }
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpenClaims(CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetOpenClaimsAsync(cancellationToken);
            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving open claims");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving open claims");
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] string? status,
        [FromQuery] string? serviceType,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? technicianId,
        [FromQuery] Guid? warrantyRegistrationId,
        [FromQuery] DateTime? claimDateFrom,
        [FromQuery] DateTime? claimDateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (claims, totalCount) = await _claimRepository.SearchPagedAsync(
                searchTerm, status, serviceType, customerId, technicianId, warrantyRegistrationId,
                claimDateFrom, claimDateTo, pageNumber, pageSize, cancellationToken);

            var response = await MapToResponsesWithLogisticsAsync(claims, cancellationToken);

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
            _logger.LogError(ex, "Error searching warranty claims");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching warranty claims");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var warranty = await _warrantyRepository.GetByIdAsync(request.WarrantyRegistrationId, cancellationToken);
            if (warranty == null)
                return BadRequest(new { message = "Warranty registration not found" });

            if (!warranty.IsValid())
                return BadRequest(new { message = $"Warranty is not valid. Status: {warranty.Status}" });

            if (request.CustomerId != warranty.CustomerId)
                return BadRequest(new { message = "Warranty does not belong to the specified customer" });

            // Fix #1: use effectiveClaimDate for both expiry check and stored claim date
            var effectiveClaimDate = request.ClaimDate == default ? DateTime.UtcNow : request.ClaimDate;
            if (effectiveClaimDate > warranty.WarrantyExpiryDate)
                return BadRequest(new
                {
                    message = $"Warranty expired on {warranty.WarrantyExpiryDate:yyyy-MM-dd}. New claims are not allowed."
                });

            var activeStatuses = new[] { "PENDING", "UNDER_REVIEW", "APPROVED", "IN_PROGRESS" };
            var existingClaims = await _claimRepository.GetByWarrantyRegistrationIdAsync(request.WarrantyRegistrationId, cancellationToken);
            var activeClaim = existingClaims
                .FirstOrDefault(c => activeStatuses.Contains(c.Status, StringComparer.OrdinalIgnoreCase));

            if (activeClaim != null)
                return BadRequest(new { message = $"Warranty already has an active claim: {activeClaim.ClaimNumber}" });

            // Cross-flow: block if this sold line was already refunded via a sales return.
            var salesReturns = await _salesReturnRepository.GetBySalesOrderAsync(warranty.SalesOrderId, cancellationToken);
            var refundedReturn = salesReturns
                .Where(r => r.Status == "PROCESSED" && r.RefundAmount > 0)
                .FirstOrDefault(r => r.LineItems.Any(li => li.SalesOrderLineId == warranty.SalesOrderLineId));

            if (refundedReturn != null)
                return BadRequest(new
                {
                    message = $"Item already refunded via sales return {refundedReturn.ReturnNumber}. Warranty claim cannot be created."
                });

            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return BadRequest(new { message = "Customer not found" });

            var claimNumber = await _warrantyService.GenerateClaimNumberAsync(cancellationToken);

            var claim = WarrantyClaim.Create(
                claimNumber: claimNumber,
                warrantyRegistrationId: request.WarrantyRegistrationId,
                customerId: request.CustomerId,
                claimDate: effectiveClaimDate,   // Fix #1
                issueDescription: request.IssueDescription,
                serviceType: request.ServiceType,
                serviceCostCurrency: request.ServiceCostCurrency
            );

            await _claimRepository.AddAsync(claim, cancellationToken);

            if (warranty.Status == "ACTIVE")
            {
                warranty.MarkAsClaimed();
                await _warrantyRepository.UpdateAsync(warranty, cancellationToken);
            }

            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warranty claim");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the warranty claim");
        }
    }

    [HttpPatch("{id:guid}/submit-for-review")]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            claim.SubmitForReview();
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim for review: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while submitting the claim for review");
        }
    }

    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            claim.Approve(request.ApprovedBy);
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while approving the claim");
        }
    }

    [HttpPatch("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Fix #6: rejection + warranty reactivation handled atomically in service
            var claim = await _warrantyService.RejectClaimAsync(id, request.RejectionReason, request.RejectedBy, cancellationToken);
            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while rejecting the claim");
        }
    }

    [HttpPatch("{id:guid}/assign-technician")]
    public async Task<IActionResult> AssignTechnician(Guid id, [FromBody] AssignTechnicianRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            // Validate technician exists
            var technician = await _technicianRepository.GetByIdAsync(request.TechnicianId, cancellationToken);
            if (technician == null)
                return BadRequest(new { message = "Technician not found" });

            claim.AssignTechnician(request.TechnicianId);
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning technician to claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while assigning the technician");
        }
    }

    [HttpPatch("{id:guid}/update-service-cost")]
    public async Task<IActionResult> UpdateServiceCost(Guid id, [FromBody] UpdateServiceCostRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            claim.UpdateServiceCost(request.ServiceCost, request.ServiceNotes);
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service cost for claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the service cost");
        }
    }

    [HttpPatch("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actor = _currentUserService.GetCurrentUsername();
            if (string.IsNullOrWhiteSpace(actor))
                actor = "system";

            // Fix #5: all stock movements, payments, and claim completion handled atomically in service
            var claim = await _warrantyService.CompleteClaimAsync(
                claimId: id,
                resolutionDetails: request.ResolutionDetails,
                refundType: request.RefundType,
                refundAmount: request.RefundAmount,
                referenceNumber: request.ReferenceNumber,
                refundNotes: request.RefundNotes,
                returnItemReceived: request.ReturnItemReceived,
                restockAsSellable: request.RestockAsSellable,
                actor: actor,
                cancellationToken: cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while completing the claim");
        }
    }

    [HttpPatch("{id:guid}/defective/send")]
    public async Task<IActionResult> SendDefectiveItem(Guid id, [FromBody] SendDefectiveItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Destination) || string.IsNullOrWhiteSpace(request.ResponsibleBy))
                return BadRequest(new { message = "Destination and ResponsibleBy are required" });

            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            if (!claim.ServiceType.Equals("REPLACEMENT", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Defective item send tracking is only valid for replacement claims" });

            var warranty = await _warrantyRepository.GetByIdAsync(claim.WarrantyRegistrationId, cancellationToken);
            if (warranty == null)
                return BadRequest(new { message = "Warranty registration not found" });

            var claimMovements = (await _stockMovementRepository.GetByReferenceNumberAsync(claim.ClaimNumber, cancellationToken)).ToList();
            var hasDefectiveReturn = claimMovements.Any(m => m.Reason == WarrantyDefectiveReturnReason);
            var hasSentToVendor = claimMovements.Any(m => m.Reason == WarrantyDefectiveSentToVendorReason);
            var hasReplacementReceived = claimMovements.Any(m => m.Reason == WarrantyReplacementReceivedFromVendorReason);

            if (!hasDefectiveReturn)
                return BadRequest(new { message = "Defective item is not yet quarantined for this claim" });

            if (hasSentToVendor && !hasReplacementReceived)
                return BadRequest(new { message = "Defective item already sent. Receive replacement before sending again" });

            if (hasReplacementReceived)
                return BadRequest(new { message = "Replacement already received for this claim. Send operation is closed" });

            var defectiveReturnMovement = claimMovements
                .Where(m => m.Reason == WarrantyDefectiveReturnReason)
                .OrderByDescending(m => m.MovementDate)
                .FirstOrDefault();

            if (defectiveReturnMovement == null)
                return BadRequest(new { message = "No quarantined defective stock movement found for this claim" });

            var stockLevel = await _stockLevelRepository.GetByIdAsync(defectiveReturnMovement.StockLevelId, cancellationToken);
            if (stockLevel == null || !stockLevel.IsActive)
                return BadRequest(new { message = "Quarantined stock location is not available for this claim" });

            if (stockLevel.QuantityReserved < 1)
                return BadRequest(new { message = "No reserved defective quantity available for this claim location" });

            stockLevel.ReleaseReservedStock(1);
            stockLevel.RemoveStock(1);
            await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

            var outMovement = StockMovement.Create(
                stockLevelId: stockLevel.Id,
                movementType: "OUT",
                quantity: 1,
                reason: WarrantyDefectiveSentToVendorReason,
                referenceNumber: claim.ClaimNumber);
            outMovement.Approve(request.ResponsibleBy);
            outMovement.AddNotes($"Sent defective item for claim {claim.ClaimNumber} to {request.Destination}. Responsible: {request.ResponsibleBy}. Ref: {request.ReferenceNumber}. {request.Notes}".Trim());
            await _stockMovementRepository.AddAsync(outMovement, cancellationToken);

            return Ok(new
            {
                message = "Defective item sent and tracked successfully",
                claimNumber = claim.ClaimNumber,
                destination = request.Destination,
                responsibleBy = request.ResponsibleBy,
                referenceNumber = request.ReferenceNumber
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending defective item for claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while sending the defective item");
        }
    }

    [HttpPatch("{id:guid}/replacement/receive")]
    public async Task<IActionResult> ReceiveReplacementItem(Guid id, [FromBody] ReceiveReplacementItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Source) || string.IsNullOrWhiteSpace(request.ResponsibleBy))
                return BadRequest(new { message = "Source and ResponsibleBy are required" });

            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            if (!claim.ServiceType.Equals("REPLACEMENT", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Replacement receive tracking is only valid for replacement claims" });

            var warranty = await _warrantyRepository.GetByIdAsync(claim.WarrantyRegistrationId, cancellationToken);
            if (warranty == null)
                return BadRequest(new { message = "Warranty registration not found" });

            var claimMovements = (await _stockMovementRepository.GetByReferenceNumberAsync(claim.ClaimNumber, cancellationToken)).ToList();
            var hasSentToVendor = claimMovements.Any(m => m.Reason == WarrantyDefectiveSentToVendorReason);
            var hasReplacementReceived = claimMovements.Any(m => m.Reason == WarrantyReplacementReceivedFromVendorReason);

            if (!hasSentToVendor)
                return BadRequest(new { message = "Defective item has not been sent yet for this claim" });

            if (hasReplacementReceived)
                return BadRequest(new { message = "Replacement item already received for this claim" });

            var sentMovement = claimMovements
                .Where(m => m.Reason == WarrantyDefectiveSentToVendorReason)
                .OrderByDescending(m => m.MovementDate)
                .FirstOrDefault();

            if (sentMovement == null)
                return BadRequest(new { message = "No sent movement found for this claim" });

            var stockLevel = await _stockLevelRepository.GetByIdAsync(sentMovement.StockLevelId, cancellationToken);
            if (stockLevel == null || !stockLevel.IsActive)
                return BadRequest(new { message = "Original claim stock location is not available for receiving replacement" });

            stockLevel.AddStock(1);
            await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

            var inMovement = StockMovement.Create(
                stockLevelId: stockLevel.Id,
                movementType: "IN",
                quantity: 1,
                reason: WarrantyReplacementReceivedFromVendorReason,
                referenceNumber: claim.ClaimNumber);
            inMovement.Approve(request.ResponsibleBy);
            inMovement.AddNotes($"Replacement item received for claim {claim.ClaimNumber} from {request.Source}. Responsible: {request.ResponsibleBy}. Ref: {request.ReferenceNumber}. {request.Notes}".Trim());
            await _stockMovementRepository.AddAsync(inMovement, cancellationToken);

            return Ok(new
            {
                message = "Replacement item received and added to sellable stock",
                claimNumber = claim.ClaimNumber,
                source = request.Source,
                responsibleBy = request.ResponsibleBy,
                referenceNumber = request.ReferenceNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving replacement item for claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while receiving the replacement item");
        }
    }

    [HttpGet("{id:guid}/replacement-logistics")]
    public async Task<IActionResult> GetReplacementLogistics(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            var isReplacement = claim.ServiceType.Equals("REPLACEMENT", StringComparison.OrdinalIgnoreCase);
            var isRefund = claim.ServiceType.Equals("REFUND", StringComparison.OrdinalIgnoreCase);

            if (!isReplacement && !isRefund)
            {
                return Ok(new WarrantyReplacementLogisticsResponse
                {
                    ClaimId = claim.Id,
                    ClaimNumber = claim.ClaimNumber,
                    State = "NOT_APPLICABLE",
                    CanSendDefectiveItem = false,
                    CanReceiveReplacementItem = false,
                    Events = new List<WarrantyReplacementLogisticsEvent>()
                });
            }

            var movements = (await _stockMovementRepository.GetByReferenceNumberAsync(claim.ClaimNumber, cancellationToken))
                .Where(m =>
                    (isReplacement && (
                        m.Reason == WarrantyReplacementOutReason ||
                        m.Reason == WarrantyDefectiveReturnReason ||
                        m.Reason == WarrantyDefectiveSentToVendorReason ||
                        m.Reason == WarrantyReplacementReceivedFromVendorReason)) ||
                    (isRefund && m.Reason == WarrantyRefundReturnReason))
                .OrderByDescending(m => m.MovementDate)
                .ToList();

            var hasDefectiveReturn = movements.Any(m => m.Reason == WarrantyDefectiveReturnReason);
            var hasSentToVendor = movements.Any(m => m.Reason == WarrantyDefectiveSentToVendorReason);
            var hasReplacementReceived = movements.Any(m => m.Reason == WarrantyReplacementReceivedFromVendorReason);
            var hasRefundReturn = movements.Any(m => m.Reason == WarrantyRefundReturnReason);

            var state = "PENDING_COMPLETION";
            if (isRefund)
            {
                state = hasRefundReturn ? "REFUND_ITEM_RETURNED" : "NOT_APPLICABLE";
            }
            else if (hasReplacementReceived)
                state = "REPLACEMENT_RECEIVED";
            else if (hasSentToVendor)
                state = "DEFECTIVE_SENT";
            else if (hasDefectiveReturn)
                state = "DEFECTIVE_QUARANTINED";

            var response = new WarrantyReplacementLogisticsResponse
            {
                ClaimId = claim.Id,
                ClaimNumber = claim.ClaimNumber,
                State = state,
                CanSendDefectiveItem = isReplacement && hasDefectiveReturn && !hasSentToVendor && !hasReplacementReceived,
                CanReceiveReplacementItem = isReplacement && hasSentToVendor && !hasReplacementReceived,
                Events = movements.Select(m => new WarrantyReplacementLogisticsEvent
                {
                    MovementId = m.Id,
                    MovementType = m.MovementType,
                    Reason = m.Reason,
                    Quantity = m.Quantity,
                    MovementDate = m.MovementDate,
                    ApprovedBy = m.ApprovedBy,
                    ReferenceNumber = m.ReferenceNumber,
                    Notes = m.Notes,
                    PartId = m.StockLevel?.PartId,
                    PartName = m.StockLevel?.Part?.Name,
                    PartSku = m.StockLevel?.Part?.SKU,
                    WarehouseId = m.StockLevel?.WarehouseId,
                    WarehouseName = m.StockLevel?.Warehouse?.Name
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving replacement logistics for claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving replacement logistics");
        }
    }

    [HttpPatch("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseClaimRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _warrantyService.CloseClaimAsync(id, request?.ClosureNotes, cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while closing the claim");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            // Fix #7: block deletion if the claim has associated financial records.
            var openStatuses = new[] { "PENDING", "UNDER_REVIEW", "APPROVED", "IN_PROGRESS" };
            if (openStatuses.Contains(claim.Status, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { message = $"Cannot delete an active claim (status: {claim.Status}). Close or reject it first." });

            if (claim.ServiceType.Equals("REFUND", StringComparison.OrdinalIgnoreCase) &&
                claim.Status == "COMPLETED")
            {
                var refundPayment = await _customerPaymentRepository.GetByTransactionNumberAsync(
                    $"WREFUND-{claim.ClaimNumber}", cancellationToken);
                if (refundPayment != null)
                    return BadRequest(new
                    {
                        message = $"Cannot delete claim {claim.ClaimNumber}: a cash refund payment ({refundPayment.TransactionNumber}) is linked to it."
                    });

                var creditNotes = await _customerCreditNoteRepository.GetByCustomerIdAsync(claim.CustomerId, cancellationToken);
                if (creditNotes.Any(cn => cn.WarrantyClaimId == claim.Id))
                    return BadRequest(new
                    {
                        message = $"Cannot delete claim {claim.ClaimNumber}: a store credit note is linked to it."
                    });
            }

            await _claimRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warranty claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the warranty claim");
        }
    }

    private async Task<List<WarrantyClaimResponse>> MapToResponsesWithLogisticsAsync(
        IEnumerable<WarrantyClaim> claims,
        CancellationToken cancellationToken)
    {
        var responses = new List<WarrantyClaimResponse>();

        foreach (var claim in claims)
        {
            responses.Add(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }

        return responses;
    }

    private async Task<WarrantyClaimResponse> MapToResponseWithLogisticsAsync(
        WarrantyClaim claim,
        CancellationToken cancellationToken)
    {
        var response = MapToResponse(claim);

        if (claim.ServiceType.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
        {
            var refundPayment = await _customerPaymentRepository.GetByTransactionNumberAsync($"WREFUND-{claim.ClaimNumber}", cancellationToken);
            if (refundPayment != null && refundPayment.WarrantyClaimId == claim.Id)
            {
                response.RefundType = "CASH_REFUND";
                response.RefundAmount = Math.Abs(refundPayment.Amount);
                response.RefundReferenceNumber = refundPayment.ReferenceNumber;
            }
            else
            {
                var creditNotes = await _customerCreditNoteRepository.GetByCustomerIdAsync(claim.CustomerId, cancellationToken);
                var creditNote = creditNotes
                    .Where(x => x.WarrantyClaimId == claim.Id ||
                        (x.WarrantyClaimId == null && !string.IsNullOrWhiteSpace(x.Notes) && x.Notes.Contains(claim.ClaimNumber, StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(x => x.IssueDate)
                    .FirstOrDefault();

                if (creditNote != null)
                {
                    response.RefundType = "STORE_CREDIT";
                    response.RefundAmount = creditNote.TotalAmount;
                    response.RefundReferenceNumber = creditNote.CreditNoteNumber;
                }
                else if (refundPayment != null)
                {
                    // Fallback for old records created before WarrantyClaimId existed.
                    response.RefundType = "CASH_REFUND";
                    response.RefundAmount = Math.Abs(refundPayment.Amount);
                    response.RefundReferenceNumber = refundPayment.ReferenceNumber;
                }
            }

            var refundReturnMovement = (await _stockMovementRepository.GetByReferenceNumberAsync(claim.ClaimNumber, cancellationToken))
                .Where(m => m.Reason == WarrantyRefundReturnReason)
                .OrderByDescending(m => m.MovementDate)
                .FirstOrDefault();

            response.RefundReturnItemReceived = refundReturnMovement != null;
            response.RefundRestockAsSellable = refundReturnMovement == null
                ? null
                : refundReturnMovement.Notes.Contains("sellable stock", StringComparison.OrdinalIgnoreCase);
        }

        var isReplacement = claim.ServiceType.Equals("REPLACEMENT", StringComparison.OrdinalIgnoreCase);
        var isRefund = claim.ServiceType.Equals("REFUND", StringComparison.OrdinalIgnoreCase);

        if (!isReplacement && !isRefund)
        {
            response.ReplacementLogisticsState = "NOT_APPLICABLE";
            response.CanSendDefectiveItem = false;
            response.CanReceiveReplacementItem = false;
            return response;
        }

        var movements = (await _stockMovementRepository.GetByReferenceNumberAsync(claim.ClaimNumber, cancellationToken))
            .Where(m =>
                (isReplacement && (
                    m.Reason == WarrantyReplacementOutReason ||
                    m.Reason == WarrantyDefectiveReturnReason ||
                    m.Reason == WarrantyDefectiveSentToVendorReason ||
                    m.Reason == WarrantyReplacementReceivedFromVendorReason)) ||
                (isRefund && m.Reason == WarrantyRefundReturnReason))
            .ToList();

        var hasDefectiveReturn = movements.Any(m => m.Reason == WarrantyDefectiveReturnReason);
        var hasSentToVendor = movements.Any(m => m.Reason == WarrantyDefectiveSentToVendorReason);
        var hasReplacementReceived = movements.Any(m => m.Reason == WarrantyReplacementReceivedFromVendorReason);
        var hasRefundReturn = movements.Any(m => m.Reason == WarrantyRefundReturnReason);

        if (isRefund)
        {
            response.ReplacementLogisticsState = hasRefundReturn ? "REFUND_ITEM_RETURNED" : "NOT_APPLICABLE";
            response.CanSendDefectiveItem = false;
            response.CanReceiveReplacementItem = false;
        }
        else
        {
            response.ReplacementLogisticsState = "PENDING_COMPLETION";
            if (hasReplacementReceived)
                response.ReplacementLogisticsState = "REPLACEMENT_RECEIVED";
            else if (hasSentToVendor)
                response.ReplacementLogisticsState = "DEFECTIVE_SENT";
            else if (hasDefectiveReturn)
                response.ReplacementLogisticsState = "DEFECTIVE_QUARANTINED";

            response.CanSendDefectiveItem = hasDefectiveReturn && !hasSentToVendor && !hasReplacementReceived;
            response.CanReceiveReplacementItem = hasSentToVendor && !hasReplacementReceived;
        }

        return response;
    }

    private WarrantyClaimResponse MapToResponse(WarrantyClaim claim)
    {
        var daysOpen = (DateTime.UtcNow - claim.ClaimDate).Days;

        return new WarrantyClaimResponse
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            WarrantyRegistrationId = claim.WarrantyRegistrationId,
            WarrantyNumber = claim.WarrantyRegistration?.WarrantyNumber ?? "",
            WarrantyCoverageType = claim.WarrantyRegistration?.WarrantyType ?? "",
            GuaranteeMessage = BuildGuaranteeMessage(claim.WarrantyRegistration),
            PartName = claim.WarrantyRegistration?.Part?.Name ?? "",
            PartSKU = claim.WarrantyRegistration?.Part?.SKU ?? "",
            CustomerId = claim.CustomerId,
            CustomerName = claim.Customer != null ? $"{claim.Customer.FirstName} {claim.Customer.LastName}" : "",
            CustomerPhone = claim.Customer?.Phone ?? "",
            TechnicianId = claim.TechnicianId,
            TechnicianName = claim.Technician?.Name,
            ClaimDate = claim.ClaimDate,
            IssueDescription = claim.IssueDescription,
            ServiceType = claim.ServiceType,
            Status = claim.Status,
            RejectionReason = claim.RejectionReason,
            RejectedDate = claim.RejectedDate,
            ApprovedDate = claim.ApprovedDate,
            ApprovedBy = claim.ApprovedBy,
            ServiceStartDate = claim.ServiceStartDate,
            ServiceCompletedDate = claim.ServiceCompletedDate,
            ServiceCost = claim.ServiceCost,
            ServiceCostCurrency = claim.ServiceCostCurrency,
            ServiceNotes = claim.ServiceNotes,
            ResolutionDetails = claim.ResolutionDetails,
            IsOpen = claim.IsOpen(),
            CanBeModified = claim.CanBeModified(),
            DaysOpen = daysOpen,
            CreatedDate = claim.CreatedDate,
            CreatedBy = claim.CreatedBy,
            ModifiedDate = claim.ModifiedDate,
            ModifiedBy = claim.ModifiedBy
        };
    }

    private static string BuildGuaranteeMessage(WarrantyRegistration? warranty)
    {
        if (warranty == null)
            return string.Empty;

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

public class ApproveClaimRequest
{
    public string ApprovedBy { get; set; } = string.Empty;
}

public class RejectClaimRequest
{
    public string RejectionReason { get; set; } = string.Empty;
    public string RejectedBy { get; set; } = string.Empty;
}

public class AssignTechnicianRequest
{
    public Guid TechnicianId { get; set; }
}

public class UpdateServiceCostRequest
{
    public decimal ServiceCost { get; set; }
    public string? ServiceNotes { get; set; }
}

public class CompleteClaimRequest
{
    public string ResolutionDetails { get; set; } = string.Empty;
    public string? RefundType { get; set; } // CASH_REFUND, STORE_CREDIT
    public decimal? RefundAmount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? RefundNotes { get; set; }
    public bool ReturnItemReceived { get; set; }
    public bool RestockAsSellable { get; set; }
}

public class CloseClaimRequest
{
    public string? ClosureNotes { get; set; }
}

public class SendDefectiveItemRequest
{
    public string Destination { get; set; } = string.Empty; // Supplier or manufacturer name
    public string ResponsibleBy { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class ReceiveReplacementItemRequest
{
    public string Source { get; set; } = string.Empty; // Supplier or manufacturer name
    public string ResponsibleBy { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class WarrantyReplacementLogisticsResponse
{
    public Guid ClaimId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string State { get; set; } = "NOT_APPLICABLE";
    public bool CanSendDefectiveItem { get; set; }
    public bool CanReceiveReplacementItem { get; set; }
    public List<WarrantyReplacementLogisticsEvent> Events { get; set; } = new();
}

public class WarrantyReplacementLogisticsEvent
{
    public Guid MovementId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime MovementDate { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid? PartId { get; set; }
    public string? PartName { get; set; }
    public string? PartSku { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}
