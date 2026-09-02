using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CreditNoteDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CreditNoteListQuery = AutoPartShop.Application.DTOs.CreditNoteDtos.CreditNoteListQuery;

namespace AutoPartShop.Api.Controllers;
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.ProcurementView)]
public class CreditNoteController : ControllerBase
{
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierPaymentRepository _supplierPaymentRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrencyConversionService _currencyConversionService;
    private readonly ILogger<CreditNoteController> _logger;

    public CreditNoteController(
        ICreditNoteRepository creditNoteRepository,
        ISupplierRepository supplierRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierPaymentRepository supplierPaymentRepository,
        AutoPartDbContext dbContext,
        ICurrentUserService currentUserService,
        ICurrencyConversionService currencyConversionService,
        ILogger<CreditNoteController> logger)
    {
        _creditNoteRepository = creditNoteRepository;
        _supplierRepository = supplierRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierPaymentRepository = supplierPaymentRepository;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _currencyConversionService = currencyConversionService;
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
    [HasPermission(Permissions.ProcurementEdit)]
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

            CreditNote? appliedCreditNote = null;
            string poNumber = string.Empty;

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var creditNote = await _creditNoteRepository.GetByIdAsync(request.CreditNoteId, cancellationToken);
                    if (creditNote is null)
                        throw new ArgumentException("Credit note not found");

                    if (!creditNote.IsAvailable())
                        throw new InvalidOperationException("This credit note is not available for use");

                    if (request.AmountToApply > creditNote.AvailableAmount)
                        throw new InvalidOperationException($"Insufficient credit available. Available: {creditNote.AvailableAmount}");

                    var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId, cancellationToken);
                    if (purchaseOrder is null)
                        throw new ArgumentException("Purchase order not found");

                    if (purchaseOrder.SupplierId != creditNote.SupplierId)
                        throw new InvalidOperationException("Credit note supplier does not match purchase order supplier");

                    // Apply credit to PO
                    creditNote.ApplyToPurchaseOrder(request.PurchaseOrderId, request.AmountToApply);
                    await _creditNoteRepository.UpdateAsync(creditNote, cancellationToken);

                    // The credit was recognised as an ADVANCE SupplierPayment when the note was issued
                    // (PurchaseReturnController.IssueCreditNote writes a COMPLETED payment with
                    // TransactionNumber == credit note number, PaymentMethod "CREDIT_NOTE"). That advance's
                    // RemainingAmount powers GetAvailableAdvanceCreditBySupplierAsync — if we don't reduce
                    // it here the same credit stays "available" in the supplier ledger after being applied
                    // to a PO, i.e. it can be spent twice.
                    var linkedPayment = await _supplierPaymentRepository.GetByTransactionNumberAsync(
                        creditNote.CreditNoteNumber, cancellationToken);
                    if (linkedPayment is not null)
                    {
                        if (linkedPayment.SupplierId != creditNote.SupplierId)
                            throw new InvalidOperationException("Linked supplier payment does not match credit note supplier");

                        linkedPayment.ReduceRemainingAmount(request.AmountToApply);
                        await _supplierPaymentRepository.UpdateAsync(linkedPayment, cancellationToken);
                    }

                    // Update PO outstanding amount
                    purchaseOrder.ApplyCredit(request.AmountToApply);
                    purchaseOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    appliedCreditNote = creditNote;
                    poNumber = purchaseOrder.PONumber;
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            _logger.LogInformation(
                "Credit note {CreditNoteNumber} applied to PO {PONumber}, amount: {Amount}",
                appliedCreditNote!.CreditNoteNumber, poNumber, request.AmountToApply);

            return Ok(MapToResponse(appliedCreditNote));
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
    [HasPermission(Permissions.ProcurementEdit)]
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

            // Retract the paired advance payment so a cancelled note's credit no longer shows as
            // "available" in GetAvailableAdvanceCreditBySupplierAsync (which sums RemainingAmount).
            var linkedPayment = await _supplierPaymentRepository.GetByTransactionNumberAsync(
                creditNote.CreditNoteNumber, cancellationToken);
            if (linkedPayment is not null && linkedPayment.PaymentType == PaymentType.ADVANCE
                && linkedPayment.RemainingAmount > 0)
            {
                linkedPayment.ReduceRemainingAmount(linkedPayment.RemainingAmount);
                await _supplierPaymentRepository.UpdateAsync(linkedPayment, cancellationToken);
            }

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
