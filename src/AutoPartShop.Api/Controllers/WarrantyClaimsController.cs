using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.WarrantyDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesView)]
public class WarrantyClaimsController : ControllerBase
{
    private const string WarrantyReplacementOutReason = "WARRANTY_REPLACEMENT_OUT";
    private const string WarrantyDefectiveReturnReason = "WARRANTY_DEFECTIVE_RETURN";
    private const string WarrantyRefundReturnReason = "WARRANTY_REFUND_RETURN";
    private const string WarrantyDefectiveSentToVendorReason = "WARRANTY_DEFECTIVE_SENT_TO_VENDOR";
    private const string WarrantyReplacementReceivedFromVendorReason = "WARRANTY_REPLACEMENT_RECEIVED_FROM_VENDOR";
    private const string WarrantyDefectiveScrappedReason = "WARRANTY_DEFECTIVE_SCRAPPED";
    private const string WarrantyDefectiveRestockedReason = "WARRANTY_DEFECTIVE_RESTOCKED";
    private const string WarrantyRepairPartsUsedReason = "WARRANTY_REPAIR_PARTS_USED";

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
    private readonly IWarrantyClaimNotifier _notifier;
    private readonly IWarrantyClaimEventRepository _claimEventRepository;
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
        IWarrantyClaimNotifier notifier,
        IWarrantyClaimEventRepository claimEventRepository,
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
        _notifier = notifier;
        _claimEventRepository = claimEventRepository;
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
    [HasPermission(Permissions.SalesCreate)]
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

            await _notifier.ClaimReceivedAsync(claim, cancellationToken);

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
    [HasPermission(Permissions.SalesEdit)]
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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            claim.Approve(request.ApprovedBy);
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            await _notifier.ClaimApprovedAsync(claim, cancellationToken);

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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Fix #6: rejection + warranty reactivation handled atomically in service
            var claim = await _warrantyService.RejectClaimAsync(id, request.RejectionReason, request.RejectedBy, cancellationToken);
            await _notifier.ClaimRejectedAsync(claim, request.RejectionReason, cancellationToken);
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
    [HasPermission(Permissions.SalesEdit)]
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

    /// <summary>
    /// Starts a repair without assigning an in-house technician (e.g. the unit is being sent to the
    /// manufacturer). Moves an APPROVED repair claim to IN_PROGRESS so it can be worked and completed.
    /// </summary>
    [HttpPatch("{id:guid}/start-service")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> StartService(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            claim.StartServiceWithoutTechnician();
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service for claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while starting service");
        }
    }

    [HttpPatch("{id:guid}/update-service-cost")]
    [HasPermission(Permissions.SalesEdit)]
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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actor = _currentUserService.GetCurrentUsername();
            if (string.IsNullOrWhiteSpace(actor))
                actor = "system";

            // Don't let a repair be completed while the customer's item is still out at the manufacturer.
            if (await IsItemOutForRepairAsync(id, cancellationToken))
                return BadRequest(new { message = "The item is still out for repair. Receive it back before completing the claim." });

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
                replacementFromVendor: request.ReplacementFromVendor,
                actor: actor,
                cancellationToken: cancellationToken);

            await _notifier.ClaimCompletedAsync(claim, cancellationToken);

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
    [Authorize(Roles = "Admin,Manager")]
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

            // Stock mutation + audit movement must commit together (under the retry strategy). Otherwise a
            // partial failure would deduct stock without the movement the idempotency guard above relies on,
            // breaking that guard and risking a double deduction on retry. RowVersion also serialises
            // concurrent sends so only one deduction can win.
            var sendLevelId = defectiveReturnMovement.StockLevelId;
            var sendStrategy = _dbContext.Database.CreateExecutionStrategy();
            await sendStrategy.ExecuteAsync(async () =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var level = await _stockLevelRepository.GetByIdAsync(sendLevelId, cancellationToken)
                        ?? throw new InvalidOperationException("Quarantined stock location is not available for this claim");

                    level.ReleaseReservedStock(1);
                    level.RemoveStock(1);
                    await _stockLevelRepository.UpdateAsync(level, cancellationToken);

                    var outMovement = StockMovement.Create(
                        stockLevelId: level.Id,
                        movementType: "OUT",
                        quantity: 1,
                        reason: WarrantyDefectiveSentToVendorReason,
                        referenceNumber: claim.ClaimNumber);
                    outMovement.Approve(request.ResponsibleBy);
                    outMovement.AddNotes($"Sent defective item for claim {claim.ClaimNumber} to {request.Destination}. Responsible: {request.ResponsibleBy}. Ref: {request.ReferenceNumber}. {request.Notes}".Trim());
                    await _stockMovementRepository.AddAsync(outMovement, cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

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
    [Authorize(Roles = "Admin,Manager")]
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

            // Stock add + audit movement must commit together (under the retry strategy) so the
            // movement-based "already received" guard above can't be bypassed by a partial failure,
            // and RowVersion serialises concurrent receives to a single +1.
            var receiveLevelId = sentMovement.StockLevelId;
            var receiveStrategy = _dbContext.Database.CreateExecutionStrategy();
            await receiveStrategy.ExecuteAsync(async () =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var level = await _stockLevelRepository.GetByIdAsync(receiveLevelId, cancellationToken)
                        ?? throw new InvalidOperationException("Original claim stock location is not available for receiving replacement");

                    level.AddStock(1);
                    await _stockLevelRepository.UpdateAsync(level, cancellationToken);

                    var inMovement = StockMovement.Create(
                        stockLevelId: level.Id,
                        movementType: "IN",
                        quantity: 1,
                        reason: WarrantyReplacementReceivedFromVendorReason,
                        referenceNumber: claim.ClaimNumber);
                    inMovement.Approve(request.ResponsibleBy);
                    inMovement.AddNotes($"Replacement item received for claim {claim.ClaimNumber} from {request.Source}. Responsible: {request.ResponsibleBy}. Ref: {request.ReferenceNumber}. {request.Notes}".Trim());
                    await _stockMovementRepository.AddAsync(inMovement, cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

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

    /// <summary>
    /// Records spare parts consumed during a repair: deducts each part/variant from stock and rolls
    /// the parts cost into the claim's service cost.
    /// </summary>
    [HttpPatch("{id:guid}/repair/parts-used")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RecordRepairParts(Guid id, [FromBody] RepairPartsUsedRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request?.Parts == null || request.Parts.Count == 0)
                return BadRequest(new { message = "At least one part is required" });
            if (request.Parts.Any(p => p.Quantity <= 0))
                return BadRequest(new { message = "Quantity must be greater than zero for every part" });

            var responsibleBy = string.IsNullOrWhiteSpace(request.ResponsibleBy)
                ? (_currentUserService.GetCurrentUsername() ?? "system")
                : request.ResponsibleBy.Trim();

            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });
            if (!claim.ServiceType.Equals("REPAIR", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Parts-used tracking is only valid for repair claims" });
            if (claim.Status != "IN_PROGRESS")
                return BadRequest(new { message = $"Parts can only be recorded while the repair is in progress. Current status: {claim.Status}" });

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Reload the claim inside the lambda so retries (which re-run this whole block after a
                    // rollback) compute cost from the committed baseline, not a mutated in-memory value.
                    var freshClaim = await _claimRepository.GetByIdAsync(id, cancellationToken)
                        ?? throw new InvalidOperationException("Warranty claim not found");
                    decimal addedCost = 0;
                    foreach (var line in request.Parts)
                    {
                        var levels = (await _stockLevelRepository.GetByPartAndVariantAsync(line.PartId, line.ProductVariantId, cancellationToken))
                            .Where(s => s.IsActive)
                            .ToList();
                        if (line.WarehouseId.HasValue)
                            levels = levels.Where(s => s.WarehouseId == line.WarehouseId.Value).ToList();

                        var source = levels.Where(s => s.QuantityAvailable >= line.Quantity)
                            .OrderByDescending(s => s.QuantityAvailable)
                            .FirstOrDefault();
                        if (source == null)
                            throw new InvalidOperationException($"Insufficient available stock for part {line.PartId} to cover {line.Quantity} unit(s).");

                        source.RemoveStock(line.Quantity);
                        await _stockLevelRepository.UpdateAsync(source, cancellationToken);

                        var movement = StockMovement.Create(
                            stockLevelId: source.Id,
                            movementType: "OUT",
                            quantity: line.Quantity,
                            reason: WarrantyRepairPartsUsedReason,
                            referenceNumber: claim.ClaimNumber);
                        movement.Approve(responsibleBy);
                        movement.AddNotes($"Spare part consumed in repair claim {claim.ClaimNumber}.");
                        await _stockMovementRepository.AddAsync(movement, cancellationToken);

                        addedCost += line.Quantity * Math.Max(0, line.UnitCost);
                    }

                    freshClaim.UpdateServiceCost(freshClaim.ServiceCost + addedCost, request.Notes);
                    await _claimRepository.UpdateAsync(freshClaim, cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                    claim = freshClaim;
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording repair parts for claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while recording repair parts");
        }
    }

    /// <summary>Logs that the customer's item was sent to a manufacturer/supplier for repair.</summary>
    [HttpPatch("{id:guid}/repair/send")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SendForRepair(Guid id, [FromBody] SendForRepairRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PartnerName) || string.IsNullOrWhiteSpace(request.ResponsibleBy))
                return BadRequest(new { message = "PartnerName and ResponsibleBy are required" });

            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });
            if (!claim.ServiceType.Equals("REPAIR", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Send-for-repair is only valid for repair claims" });
            if (claim.Status != "IN_PROGRESS" && claim.Status != "APPROVED")
                return BadRequest(new { message = $"Item can only be sent for repair on an approved/in-progress claim. Current status: {claim.Status}" });

            var events = (await _claimEventRepository.GetByClaimIdAsync(id, cancellationToken)).ToList();
            var outstanding = events.Count(e => e.EventType == "SENT_FOR_REPAIR") > events.Count(e => e.EventType == "RECEIVED_FROM_REPAIR");
            if (outstanding)
                return BadRequest(new { message = "Item is already out for repair. Receive it back before sending again." });

            // Sending the unit out IS the start of service â€” move an approved claim to in-progress so it
            // can be completed later without forcing an in-house technician assignment.
            if (claim.Status == "APPROVED")
            {
                claim.StartServiceWithoutTechnician();
                await _claimRepository.UpdateAsync(claim, cancellationToken);
            }

            var actor = _currentUserService.GetCurrentUsername() ?? "system";
            var evt = WarrantyClaimEvent.Create(
                warrantyClaimId: id,
                eventType: "SENT_FOR_REPAIR",
                partnerType: request.PartnerType,
                partnerName: request.PartnerName,
                responsibleBy: request.ResponsibleBy,
                referenceNumber: request.ReferenceNumber,
                expectedReturnDate: request.ExpectedReturnDate,
                notes: request.Notes,
                actor: actor);
            await _claimEventRepository.AddAsync(evt, cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending item for repair on claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while sending the item for repair");
        }
    }

    /// <summary>Logs that the repaired item was received back from the manufacturer/supplier.</summary>
    [HttpPatch("{id:guid}/repair/receive")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ReceiveFromRepair(Guid id, [FromBody] ReceiveFromRepairRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ResponsibleBy))
                return BadRequest(new { message = "ResponsibleBy is required" });

            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });
            if (!claim.ServiceType.Equals("REPAIR", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Receive-from-repair is only valid for repair claims" });

            var events = (await _claimEventRepository.GetByClaimIdAsync(id, cancellationToken)).ToList();
            var lastSent = events.Where(e => e.EventType == "SENT_FOR_REPAIR").OrderByDescending(e => e.EventDate).FirstOrDefault();
            var outstanding = events.Count(e => e.EventType == "SENT_FOR_REPAIR") > events.Count(e => e.EventType == "RECEIVED_FROM_REPAIR");
            if (!outstanding || lastSent == null)
                return BadRequest(new { message = "No item is currently out for repair on this claim." });

            var actor = _currentUserService.GetCurrentUsername() ?? "system";
            var evt = WarrantyClaimEvent.Create(
                warrantyClaimId: id,
                eventType: "RECEIVED_FROM_REPAIR",
                partnerType: lastSent.PartnerType,
                partnerName: lastSent.PartnerName,
                responsibleBy: request.ResponsibleBy,
                referenceNumber: request.ReferenceNumber,
                expectedReturnDate: null,
                notes: request.Notes,
                actor: actor);
            await _claimEventRepository.AddAsync(evt, cancellationToken);

            return Ok(await MapToResponseWithLogisticsAsync(claim, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving item from repair on claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while receiving the item from repair");
        }
    }

    /// <summary>
    /// Final disposition of a quarantined defective unit that is NOT going back to a vendor:
    /// scrap/write-off (removed from stock) or restock-as-sellable (release the quarantine hold).
    /// </summary>
    [HttpPatch("{id:guid}/defective/{disposition}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DisposeDefectiveItem(Guid id, string disposition, [FromBody] DisposeDefectiveItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var mode = (disposition ?? string.Empty).Trim().ToLowerInvariant();
            if (mode != "scrap" && mode != "restock")
                return BadRequest(new { message = "Disposition must be 'scrap' or 'restock'." });

            if (string.IsNullOrWhiteSpace(request.ResponsibleBy))
                return BadRequest(new { message = "ResponsibleBy is required" });

            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            if (!claim.ServiceType.Equals("REPLACEMENT", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Defective disposition is only valid for replacement claims" });

            var claimMovements = (await _stockMovementRepository.GetByReferenceNumberAsync(claim.ClaimNumber, cancellationToken)).ToList();
            var hasDefectiveReturn = claimMovements.Any(m => m.Reason == WarrantyDefectiveReturnReason);
            var hasSentToVendor = claimMovements.Any(m => m.Reason == WarrantyDefectiveSentToVendorReason);
            var hasReplacementReceived = claimMovements.Any(m => m.Reason == WarrantyReplacementReceivedFromVendorReason);
            var alreadyDisposed = claimMovements.Any(m => m.Reason == WarrantyDefectiveScrappedReason || m.Reason == WarrantyDefectiveRestockedReason);

            if (!hasDefectiveReturn)
                return BadRequest(new { message = "Defective item is not quarantined for this claim" });
            if (hasSentToVendor || hasReplacementReceived)
                return BadRequest(new { message = "Defective item is in the vendor-return flow; disposition does not apply" });
            if (alreadyDisposed)
                return BadRequest(new { message = "Defective item has already been disposed for this claim" });

            var quarantineMovement = claimMovements
                .Where(m => m.Reason == WarrantyDefectiveReturnReason)
                .OrderByDescending(m => m.MovementDate)
                .FirstOrDefault();
            if (quarantineMovement == null)
                return BadRequest(new { message = "No quarantined defective stock movement found for this claim" });

            var reason = mode == "scrap" ? WarrantyDefectiveScrappedReason : WarrantyDefectiveRestockedReason;

            // Stock level change + audit movement must be atomic, under the EF execution strategy
            // (global retry policy). Reload the stock level inside so a retry applies the change once.
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var stockLevel = await _stockLevelRepository.GetByIdAsync(quarantineMovement.StockLevelId, cancellationToken);
                    if (stockLevel == null || !stockLevel.IsActive)
                        throw new InvalidOperationException("Quarantined stock location is not available for this claim");
                    if (stockLevel.QuantityReserved < 1)
                        throw new InvalidOperationException("No reserved defective quantity available for this claim location");

                    // Both dispositions release the quarantine hold; scrap also removes the unit from stock.
                    stockLevel.ReleaseReservedStock(1);
                    if (mode == "scrap")
                        stockLevel.RemoveStock(1);
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                    var movement = StockMovement.Create(
                        stockLevelId: stockLevel.Id,
                        movementType: mode == "scrap" ? "OUT" : "IN",
                        quantity: 1,
                        reason: reason,
                        referenceNumber: claim.ClaimNumber);
                    movement.Approve(request.ResponsibleBy);
                    movement.AddNotes(mode == "scrap"
                        ? $"Defective item scrapped/written off for claim {claim.ClaimNumber}. Responsible: {request.ResponsibleBy}. {request.Notes}".Trim()
                        : $"Defective item released back to sellable stock for claim {claim.ClaimNumber}. Responsible: {request.ResponsibleBy}. {request.Notes}".Trim());
                    await _stockMovementRepository.AddAsync(movement, cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(new
            {
                message = mode == "scrap" ? "Defective item scrapped" : "Defective item returned to sellable stock",
                claimNumber = claim.ClaimNumber,
                disposition = mode
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing defective item for claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while disposing the defective item");
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
                        m.Reason == WarrantyReplacementReceivedFromVendorReason ||
                        m.Reason == WarrantyDefectiveScrappedReason ||
                        m.Reason == WarrantyDefectiveRestockedReason)) ||
                    (isRefund && m.Reason == WarrantyRefundReturnReason))
                .OrderByDescending(m => m.MovementDate)
                .ToList();

            var hasDefectiveReturn = movements.Any(m => m.Reason == WarrantyDefectiveReturnReason);
            var hasSentToVendor = movements.Any(m => m.Reason == WarrantyDefectiveSentToVendorReason);
            var hasReplacementReceived = movements.Any(m => m.Reason == WarrantyReplacementReceivedFromVendorReason);
            var hasScrapped = movements.Any(m => m.Reason == WarrantyDefectiveScrappedReason);
            var hasRestocked = movements.Any(m => m.Reason == WarrantyDefectiveRestockedReason);
            var hasRefundReturn = movements.Any(m => m.Reason == WarrantyRefundReturnReason);

            var state = "PENDING_COMPLETION";
            if (isRefund)
            {
                state = hasRefundReturn ? "REFUND_ITEM_RETURNED" : "NOT_APPLICABLE";
            }
            else if (hasReplacementReceived)
                state = "REPLACEMENT_RECEIVED";
            else if (hasScrapped)
                state = "DEFECTIVE_SCRAPPED";
            else if (hasRestocked)
                state = "DEFECTIVE_RESTOCKED";
            else if (hasSentToVendor)
                state = "DEFECTIVE_SENT";
            else if (hasDefectiveReturn)
                state = "DEFECTIVE_QUARANTINED";

            var canDispose = isReplacement && hasDefectiveReturn && !hasSentToVendor && !hasReplacementReceived && !hasScrapped && !hasRestocked;
            var response = new WarrantyReplacementLogisticsResponse
            {
                ClaimId = claim.Id,
                ClaimNumber = claim.ClaimNumber,
                State = state,
                CanSendDefectiveItem = isReplacement && hasDefectiveReturn && !hasSentToVendor && !hasReplacementReceived && !hasScrapped && !hasRestocked,
                CanReceiveReplacementItem = isReplacement && hasSentToVendor && !hasReplacementReceived,
                CanScrapDefectiveItem = canDispose,
                CanRestockDefectiveItem = canDispose,
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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseClaimRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            if (await IsItemOutForRepairAsync(id, cancellationToken))
                return BadRequest(new { message = "The item is still out for repair. Receive it back before closing the claim." });

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
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>True when a repair claim has an item sent to a partner that hasn't been received back.</summary>
    private async Task<bool> IsItemOutForRepairAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var events = (await _claimEventRepository.GetByClaimIdAsync(claimId, cancellationToken)).ToList();
        return events.Count(e => e.EventType == "SENT_FOR_REPAIR") > events.Count(e => e.EventType == "RECEIVED_FROM_REPAIR");
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

        var isRepair = claim.ServiceType.Equals("REPAIR", StringComparison.OrdinalIgnoreCase);
        if (isRepair)
        {
            var repairEvents = (await _claimEventRepository.GetByClaimIdAsync(claim.Id, cancellationToken)).ToList();
            var sentCount = repairEvents.Count(e => e.EventType == "SENT_FOR_REPAIR");
            var receivedCount = repairEvents.Count(e => e.EventType == "RECEIVED_FROM_REPAIR");
            var outstanding = sentCount > receivedCount;
            var lastSent = repairEvents.Where(e => e.EventType == "SENT_FOR_REPAIR").OrderByDescending(e => e.EventDate).FirstOrDefault();

            response.RepairLogisticsState = outstanding ? "AT_PARTNER"
                : sentCount > 0 ? "RETURNED_FROM_REPAIR"
                : "NOT_APPLICABLE";
            response.CanSendForRepair = !outstanding && (claim.Status == "APPROVED" || claim.Status == "IN_PROGRESS");
            response.CanReceiveFromRepair = outstanding;
            response.RepairExpectedReturnDate = outstanding ? lastSent?.ExpectedReturnDate : null;
        }

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
                    m.Reason == WarrantyReplacementReceivedFromVendorReason ||
                    m.Reason == WarrantyDefectiveScrappedReason ||
                    m.Reason == WarrantyDefectiveRestockedReason)) ||
                (isRefund && m.Reason == WarrantyRefundReturnReason))
            .ToList();

        var hasDefectiveReturn = movements.Any(m => m.Reason == WarrantyDefectiveReturnReason);
        var hasSentToVendor = movements.Any(m => m.Reason == WarrantyDefectiveSentToVendorReason);
        var hasReplacementReceived = movements.Any(m => m.Reason == WarrantyReplacementReceivedFromVendorReason);
        var hasScrapped = movements.Any(m => m.Reason == WarrantyDefectiveScrappedReason);
        var hasRestocked = movements.Any(m => m.Reason == WarrantyDefectiveRestockedReason);
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
            else if (hasScrapped)
                response.ReplacementLogisticsState = "DEFECTIVE_SCRAPPED";
            else if (hasRestocked)
                response.ReplacementLogisticsState = "DEFECTIVE_RESTOCKED";
            else if (hasSentToVendor)
                response.ReplacementLogisticsState = "DEFECTIVE_SENT";
            else if (hasDefectiveReturn)
                response.ReplacementLogisticsState = "DEFECTIVE_QUARANTINED";

            var canDispose = hasDefectiveReturn && !hasSentToVendor && !hasReplacementReceived && !hasScrapped && !hasRestocked;
            response.CanSendDefectiveItem = canDispose;
            response.CanReceiveReplacementItem = hasSentToVendor && !hasReplacementReceived;
            response.CanScrapDefectiveItem = canDispose;
            response.CanRestockDefectiveItem = canDispose;
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
            PartLocalName = claim.WarrantyRegistration?.Part?.LocalName,
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

    // REPLACEMENT only: when true, the replacement unit is sourced from the vendor
    // (not dispatched from on-hand stock now), so the immediate stock OUT is skipped.
    // The actual replacement is driven later by the defective/send â†’ replacement/receive flow.
    public bool ReplacementFromVendor { get; set; }
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

public class DisposeDefectiveItemRequest
{
    public string ResponsibleBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class RepairPartsUsedRequest
{
    public List<RepairPartLine> Parts { get; set; } = new();
    public string? ResponsibleBy { get; set; }
    public string? Notes { get; set; }
}

public class RepairPartLine
{
    public Guid PartId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class SendForRepairRequest
{
    public string PartnerType { get; set; } = "MANUFACTURER"; // MANUFACTURER or SUPPLIER
    public string PartnerName { get; set; } = string.Empty;
    public string ResponsibleBy { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string? Notes { get; set; }
}

public class ReceiveFromRepairRequest
{
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
    public bool CanScrapDefectiveItem { get; set; }
    public bool CanRestockDefectiveItem { get; set; }
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
