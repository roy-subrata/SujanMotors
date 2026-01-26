using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.WarrantyDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class WarrantyClaimsController : ControllerBase
{
    private readonly IWarrantyClaimRepository _claimRepository;
    private readonly IWarrantyRegistrationRepository _warrantyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ITechnicianRepository _technicianRepository;
    private readonly IWarrantyService _warrantyService;
    private readonly ILogger<WarrantyClaimsController> _logger;

    public WarrantyClaimsController(
        IWarrantyClaimRepository claimRepository,
        IWarrantyRegistrationRepository warrantyRepository,
        ICustomerRepository customerRepository,
        ITechnicianRepository technicianRepository,
        IWarrantyService warrantyService,
        ILogger<WarrantyClaimsController> logger)
    {
        _claimRepository = claimRepository;
        _warrantyRepository = warrantyRepository;
        _customerRepository = customerRepository;
        _technicianRepository = technicianRepository;
        _warrantyService = warrantyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimRepository.GetAllAsync(cancellationToken);
            var response = claims.Select(MapToResponse);
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

            return Ok(MapToResponse(claim));
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

            return Ok(MapToResponse(claim));
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
            var response = claims.Select(MapToResponse);
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
            var response = claims.Select(MapToResponse);
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
            var response = claims.Select(MapToResponse);
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
            var response = claims.Select(MapToResponse);
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
            var response = claims.Select(MapToResponse);
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
            var response = claims.Select(MapToResponse);
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
            var response = claims.Select(MapToResponse);
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

            var response = claims.Select(MapToResponse);

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
            // Validate warranty registration exists and is valid
            var warranty = await _warrantyRepository.GetByIdAsync(request.WarrantyRegistrationId, cancellationToken);
            if (warranty == null)
                return BadRequest(new { message = "Warranty registration not found" });

            if (!warranty.IsValid())
                return BadRequest(new { message = $"Warranty is not valid. Status: {warranty.Status}" });

            // Validate customer exists
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return BadRequest(new { message = "Customer not found" });

            // Generate claim number
            var claimNumber = await _warrantyService.GenerateClaimNumberAsync(cancellationToken);

            // Create warranty claim
            var claim = WarrantyClaim.Create(
                claimNumber: claimNumber,
                warrantyRegistrationId: request.WarrantyRegistrationId,
                customerId: request.CustomerId,
                claimDate: request.ClaimDate,
                issueDescription: request.IssueDescription,
                serviceType: request.ServiceType,
                serviceCostCurrency: request.ServiceCostCurrency
            );

            await _claimRepository.AddAsync(claim, cancellationToken);

            // Mark warranty as claimed if this is the first claim
            if (warranty.Status == "ACTIVE")
            {
                warranty.MarkAsClaimed();
                await _warrantyRepository.UpdateAsync(warranty, cancellationToken);
            }

            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, MapToResponse(claim));
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

            return Ok(MapToResponse(claim));
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

            return Ok(MapToResponse(claim));
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
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            claim.Reject(request.RejectionReason, request.RejectedBy);
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            return Ok(MapToResponse(claim));
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

            return Ok(MapToResponse(claim));
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

            return Ok(MapToResponse(claim));
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
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            claim.Complete(request.ResolutionDetails);
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            return Ok(MapToResponse(claim));
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

    [HttpPatch("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
            if (claim == null)
                return NotFound(new { message = "Warranty claim not found" });

            claim.Close();
            await _claimRepository.UpdateAsync(claim, cancellationToken);

            return Ok(MapToResponse(claim));
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
            if (!await _claimRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Warranty claim not found" });

            await _claimRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warranty claim: {ClaimId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the warranty claim");
        }
    }

    private WarrantyClaimResponse MapToResponse(WarrantyClaim claim)
    {
        var daysOpen = claim.ClaimDate != null
            ? (DateTime.UtcNow - claim.ClaimDate).Days
            : 0;

        return new WarrantyClaimResponse
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            WarrantyRegistrationId = claim.WarrantyRegistrationId,
            WarrantyNumber = claim.WarrantyRegistration?.WarrantyNumber ?? "",
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
}
