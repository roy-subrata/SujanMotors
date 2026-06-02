using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CreditNoteDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CreditNoteListQuery = AutoPartShop.Application.DTOs.CreditNoteDtos.CreditNoteListQuery;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[Authorize]
public class CreditNoteController : ControllerBase
{
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierPaymentRepository _supplierPaymentRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreditNoteController> _logger;

    public CreditNoteController(
        ICreditNoteRepository creditNoteRepository,
        ISupplierRepository supplierRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierPaymentRepository supplierPaymentRepository,
        AutoPartDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CreditNoteController> logger)
    {
        _creditNoteRepository = creditNoteRepository;
        _supplierRepository = supplierRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierPaymentRepository = supplierPaymentRepository;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("supplier/{supplierId:guid}")]
    public async Task<IActionResult> GetBySupplier(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var creditNotes = await _creditNoteRepository.GetBySupplierIdAsync(supplierId, cancellationToken);
            var response = creditNotes.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit notes for supplier {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving credit notes");
        }
    }

    [HttpGet("supplier/{supplierId:guid}/available")]
    public async Task<IActionResult> GetAvailableCredits(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var creditNotes = await _creditNoteRepository.GetAvailableCreditsAsync(supplierId, cancellationToken);
            var response = creditNotes.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available credits for supplier {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving available credits");
        }
    }

    [HttpGet("supplier/{supplierId:guid}/total-available")]
    public async Task<IActionResult> GetTotalAvailableCredit(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var total = await _creditNoteRepository.GetTotalAvailableCreditAsync(supplierId, cancellationToken);
            return Ok(new { totalAvailableCredit = total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total available credit for supplier {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving total available credit");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] CreditNoteListQuery query, CancellationToken cancellationToken)
    {
        try
        {
            if (query.PageNumber < 1) query.PageNumber = 1;
            if (query.PageSize < 1) query.PageSize = 10;
            if (query.PageSize > 100) query.PageSize = 100;

            var (creditNotes, totalCount) = await _creditNoteRepository.SearchPagedAsync(
                new Domain.Repositories.CreditNoteQuery
                {
                    SupplierId = query.SupplierId,
                    Status = query.Status,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                }, cancellationToken);

            var response = creditNotes.Select(MapToResponse);
            return Ok(new
            {
                data = response,
                pagination = new
                {
                    query.PageNumber,
                    query.PageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit notes list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving credit notes");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(id, cancellationToken);
            if (creditNote is null) return NotFound(new { message = "Credit note not found" });

            return Ok(MapToResponse(creditNote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit note {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the credit note");
        }
    }

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyCredit([FromBody] ApplyCreditNoteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.CreditNoteId == Guid.Empty)
                return BadRequest(new { message = "CreditNoteId is required" });

            if (request.PurchaseOrderId == Guid.Empty)
                return BadRequest(new { message = "PurchaseOrderId is required" });

            if (request.AmountToApply <= 0)
                return BadRequest(new { message = "Amount to apply must be greater than 0" });

            var creditNote = await _creditNoteRepository.GetByIdAsync(request.CreditNoteId, cancellationToken);
            if (creditNote is null) return NotFound(new { message = "Credit note not found" });

            if (!creditNote.IsAvailable())
                return BadRequest(new { message = "This credit note is not available for use" });

            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId, cancellationToken);
            if (purchaseOrder is null) return NotFound(new { message = "Purchase order not found" });

            if (purchaseOrder.SupplierId != creditNote.SupplierId)
                return BadRequest(new { message = "Credit note supplier does not match purchase order supplier" });

            // Apply credit to PO
            var remainingAvailable = creditNote.ApplyToPurchaseOrder(request.PurchaseOrderId, request.AmountToApply);
            await _creditNoteRepository.UpdateAsync(creditNote, cancellationToken);

            // Create SupplierPayment record to track the application
            var defaultProvider = await _dbContext.PaymentProviders.FirstOrDefaultAsync(cancellationToken);

            if (defaultProvider != null)
            {
                var supplierPayment = SupplierPayment.CreateFromAdvance(
                    supplierId: creditNote.SupplierId,
                    purchaseOrderId: request.PurchaseOrderId,
                    sourceAdvancePaymentId: creditNote.Id,  // Link to credit note
                    paymentProviderId: defaultProvider.Id,
                    amount: request.AmountToApply,
                    description: $"Applied credit note {creditNote.CreditNoteNumber} to PO {purchaseOrder.PONumber}"
                );
                await _supplierPaymentRepository.AddAsync(supplierPayment, cancellationToken);
            }

            // Update PO outstanding amount
            purchaseOrder.ApplyCredit(request.AmountToApply);
            purchaseOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);

            _logger.LogInformation(
                "Credit note {CreditNoteNumber} applied to PO {PONumber}, amount: {Amount}, remaining: {Remaining}",
                creditNote.CreditNoteNumber, purchaseOrder.PONumber, request.AmountToApply, remainingAvailable);

            return Ok(MapToResponse(creditNote));
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
            _logger.LogError(ex, "Error applying credit note");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while applying credit note");
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromQuery] string reason = "", CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(id, cancellationToken);
            if (creditNote is null) return NotFound(new { message = "Credit note not found" });

            if (creditNote.UsedAmount > 0)
                return BadRequest(new { message = "Cannot cancel a credit note that has been partially used" });

            creditNote.Cancel(reason);
            await _creditNoteRepository.UpdateAsync(creditNote, cancellationToken);

            return Ok(MapToResponse(creditNote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling credit note {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while cancelling credit note");
        }
    }

    private CreditNoteResponse MapToResponse(CreditNote cn)
    {
        return new CreditNoteResponse
        {
            Id = cn.Id,
            CreditNoteNumber = cn.CreditNoteNumber,
            SupplierId = cn.SupplierId,
            SupplierName = cn.Supplier?.Name ?? string.Empty,
            PurchaseReturnId = cn.PurchaseReturnId,
            ReturnNumber = cn.PurchaseReturn?.ReturnNumber,
            PurchaseOrderId = cn.PurchaseOrderId,
            PurchaseOrderNumber = cn.PurchaseOrder?.PONumber,
            TotalAmount = cn.TotalAmount,
            UsedAmount = cn.UsedAmount,
            AvailableAmount = cn.AvailableAmount,
            Currency = cn.Currency,
            IssueDate = cn.IssueDate,
            ExpiryDate = cn.ExpiryDate,
            Status = cn.Status,
            Notes = cn.Notes,
            IssuedBy = cn.IssuedBy,
            CreatedBy = cn.CreatedBy,
            CreatedAt = cn.CreatedDate
        };
    }
}
