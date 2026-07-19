using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using QuestPDF.Fluent;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.PaymentDtos;
using AutoPartShop.Application.Supplier;
using AutoPartShop.Application.SupplierPayment.Dtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/supplier-payments")]
[Route("api/v1/supplier-payments")]
[ApiController]
// procurement.create (not .view) on the whole controller keeps supplier payments
// restricted to roles that can spend — preserving the previous Admin/Manager-only posture
[HasPermission(Permissions.ProcurementCreate)]
public class SupplierPaymentController : ControllerBase
{
    private readonly ISupplierPaymentRepository _repository;
    public readonly ISupplierPaymentReadRespository _supplierPaymentReadRespository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IGoodsReceiptRepository _goodsReceiptRepository;
    private readonly SupplierPaymentSummaryService _summaryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<SupplierPaymentController> _logger;

    public SupplierPaymentController(
        ISupplierPaymentRepository repository,
        ISupplierPaymentReadRespository supplierPaymentReadRespository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IGoodsReceiptRepository goodsReceiptRepository,
        SupplierPaymentSummaryService summaryService,
        ICurrentUserService currentUserService,
        AutoPartDbContext dbContext,
        ILogger<SupplierPaymentController> logger)
    {
        _repository = repository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _summaryService = summaryService;
        _currentUserService = currentUserService;
        _supplierPaymentReadRespository = supplierPaymentReadRespository;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _repository.GetAllAsync(cancellationToken);
            return Ok(payments.Select(MapResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payments");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] SupplierPaymentQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (query == null)
            {
                return BadRequest("Query parameters are required.");
            }

            if (query.PageNumber <= 0 || query.PageSize <= 0)
            {
                return BadRequest("Invalid pagination parameters.");
            }

            var (payments, totalCount) = await _supplierPaymentReadRespository.FindAllAsynce(query, cancellationToken);

            return Ok(PagedResult<SupplierPaymentResponse>.Create(
                payments,
                totalCount,
                query
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payments list");
            return StatusCode(500, new { message = "An error occurred while retrieving supplier payments" });
        }
    }

    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _repository.GetByStatusAsync(status, cancellationToken);
            return Ok(payments.Select(MapResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payments by status");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();
            return Ok(MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("supplier/{supplierId:guid}")]
    public async Task<IActionResult> GetBySupplier(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var (payments, totalCount) = await _repository.GetBySupplierPagedAsync(supplierId, 1, 50, cancellationToken);
            return Ok(new { data = payments.Select(MapResponse), totalCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payments");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("supplier/{supplierId:guid}/summary")]
    public async Task<IActionResult> GetSummary(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _summaryService.GetSupplierPaymentSummaryAsync(supplierId, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Supplier not found: {SupplierId}", supplierId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payment summary");
            return StatusCode(500, new { message = "An error occurred while retrieving the payment summary" });
        }
    }

    [HttpGet("supplier/{supplierId:guid}/history")]
    public async Task<IActionResult> GetPaymentHistory(Guid supplierId, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;

            var history = await _summaryService.GetPaymentHistoryAsync(supplierId, limit, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payment history");
            return StatusCode(500, new { message = "An error occurred while retrieving payment history" });
        }
    }

    [HttpGet("supplier/{supplierId:guid}/status-breakdown")]
    public async Task<IActionResult> GetStatusBreakdown(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var breakdown = await _summaryService.GetStatusBreakdownAsync(supplierId, cancellationToken);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status breakdown");
            return StatusCode(500, new { message = "An error occurred while retrieving status breakdown" });
        }
    }

    [HttpGet("supplier/{supplierId:guid}/advance")]
    public async Task<IActionResult> GetAdvancePayments(Guid supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            var advancePayments = await _summaryService.GetAdvancePaymentsAsync(supplierId, cancellationToken);
            return Ok(advancePayments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting advance payments");
            return StatusCode(500, new { message = "An error occurred while retrieving advance payments" });
        }
    }

    [HttpGet("supplier/{supplierId:guid}/report")]
    public async Task<IActionResult> DownloadPaymentSummaryReport(
        Guid supplierId,
        [FromServices] IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating payment summary report for supplier: {SupplierId}", supplierId);

            var summary = await _summaryService.GetSupplierPaymentSummaryAsync(supplierId, cancellationToken);
            if (summary == null)
            {
                _logger.LogWarning("Supplier summary is null for: {SupplierId}", supplierId);
                return NotFound(new { message = "Supplier not found" });
            }

            _logger.LogInformation("Summary retrieved for supplier: {SupplierName}", summary.SupplierName);

            var shop = await shopProfiles.GetAsync(cancellationToken: cancellationToken);
            var pdfBytes = new SupplierAccountStatementDocument(summary, shop).GeneratePdf();
            _logger.LogInformation("PDF report generated, size: {Size}", pdfBytes.Length);

            return File(pdfBytes, "application/pdf", $"supplier-statement-{summary.SupplierCode}-{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Supplier not found: {SupplierId}", supplierId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment summary report for supplier: {SupplierId}", supplierId);
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierPaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validation: REGULAR payments MUST have a PurchaseOrderId
            if (request.PaymentType == PaymentType.REGULAR && !request.PurchaseOrderId.HasValue)
            {
                return BadRequest(new { message = "Regular payments must be linked to a purchase order. Use ADVANCE payment type for prepayments without a specific order." });
            }

            // A supplier invoice should only cover ACCEPTED goods. When a payment is tied to a GRN,
            // it cannot exceed the accepted value (accepted qty x unit cost) of that receipt.
            if (request.GoodsReceiptId.HasValue)
            {
                var grn = await _goodsReceiptRepository.GetByIdAsync(request.GoodsReceiptId.Value, cancellationToken);
                if (grn is null)
                    return BadRequest(new { message = "Linked goods receipt not found." });

                var acceptedValue = grn.LineItems.Sum(l => l.AcceptedTotalCost);
                if (request.Amount > acceptedValue)
                    return BadRequest(new { message = $"Payment amount ({request.Amount:N2}) exceeds the accepted value of goods receipt {grn.GRNNumber} ({acceptedValue:N2}). Rejected/damaged items are excluded from the supplier invoice." });
            }

            var payment = SupplierPayment.Create(request.SupplierId, request.PaymentProviderId, request.Amount, request.PaymentMethod, request.TransactionNumber, request.ReferenceNumber, request.PaymentDate);

            if (request.PurchaseOrderId.HasValue)
                payment.LinkToPurchaseOrder(request.PurchaseOrderId.Value);
            if (request.GoodsReceiptId.HasValue)
                payment.LinkToGoodsReceipt(request.GoodsReceiptId.Value);
            if (request.SupplierPaymentAccountId.HasValue)
                payment.SetSupplierPaymentAccount(request.SupplierPaymentAccountId.Value);
            if (!string.IsNullOrEmpty(request.InvoiceNumber))
                payment.SetInvoiceNumber(request.InvoiceNumber);

            // Set payment type based on request
            if (request.PaymentType == PaymentType.ADVANCE)
            {
                payment.MarkAsAdvance();
                if (!string.IsNullOrEmpty(request.Description))
                    payment.UpdateNotes(request.Description);
            }

            var currentUser = _currentUserService.GetCurrentUsername();
            payment.CreatedBy = currentUser;
            payment.ModifiedBy = currentUser;

            // Auto-confirm every recorded payment so it counts immediately toward the
            // supplier balance / PO outstanding (recording a payment means money has moved).
            {
                _logger.LogInformation("Auto-confirming {Method} payment of {Amount} for supplier {SupplierId}, Type: {PaymentType}",
                    request.PaymentMethod, request.Amount, request.SupplierId, request.PaymentType);
                payment.MarkAsProcessed(currentUser + " - Auto-confirmed");
                payment.ConfirmReceipt(currentUser + " - Recorded payment");

                // For REGULAR payments: Update purchase order and supplier balance
                if (request.PaymentType == PaymentType.REGULAR)
                {
                    // Wrap PO update + payment insert in a single transaction so a partial failure
                    // cannot leave one written without the other.
                    await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId!.Value, cancellationToken);
                        if (purchaseOrder != null)
                        {
                            purchaseOrder.RecordPayment(payment.Amount);
                            purchaseOrder.ModifiedBy = currentUser;
                            await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
                            _logger.LogInformation("Updated purchase order {PONumber} paid amount by {Amount}", purchaseOrder.PONumber, payment.Amount);
                        }

                        await _repository.AddAsync(payment, cancellationToken);
                        await tx.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await tx.RollbackAsync(cancellationToken);
                        throw;
                    }

                    _logger.LogInformation("Payment {TxnNumber} completed for supplier. Amount: {Amount}. Balance calculated from transactions.",
                        payment.TransactionNumber, payment.Amount);
                }
                // For ADVANCE payments: Balance is calculated from transactions
                // Advances contribute to available credit, not direct balance reduction
                else
                {
                    await _repository.AddAsync(payment, cancellationToken);
                    _logger.LogInformation("ADVANCE payment created. Supplier balance NOT updated - will be applied when used for a PO.");
                }
            }

            var created = await _repository.GetByIdAsync(payment.Id, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, MapResponse(created!));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/mark-processed")]
    public async Task<IActionResult> MarkProcessed(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            payment.MarkAsProcessed(currentUser);
            payment.ModifiedBy = currentUser;

            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _repository.UpdateAsync(payment, cancellationToken);

                if (payment.PaymentType == PaymentType.REGULAR && payment.PurchaseOrderId.HasValue)
                {
                    var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(payment.PurchaseOrderId.Value, cancellationToken);
                    if (purchaseOrder != null)
                    {
                        purchaseOrder.RecordPayment(payment.Amount);
                        purchaseOrder.ModifiedBy = currentUser;
                        await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
                        _logger.LogInformation("Updated purchase order {PONumber} paid amount by {Amount}", purchaseOrder.PONumber, payment.Amount);
                    }
                }
                else
                {
                    _logger.LogInformation("ADVANCE payment {TransactionNumber} processed. Balance calculated from transactions.",
                        payment.TransactionNumber);
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Payment {TxnNumber} marked as processed. Amount: {Amount}.", payment.TransactionNumber, payment.Amount);
            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking supplier payment as processed");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            payment.ConfirmReceipt(currentUser);
            payment.ModifiedBy = currentUser;
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming supplier payment receipt");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();
            payment.Reconcile();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling supplier payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound(new { message = "Supplier payment not found" });

            var currentUser = _currentUserService.GetCurrentUsername();

            // Track whether PO was already updated by a prior mark-processed call
            var alreadyProcessed = payment.ProcessedDate.HasValue;

            payment.MarkAsProcessed(currentUser);
            payment.ConfirmReceipt(currentUser);
            payment.ModifiedBy = currentUser;

            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _repository.UpdateAsync(payment, cancellationToken);

                // Only apply RecordPayment once — skip if mark-processed already did it
                if (payment.PaymentType == PaymentType.REGULAR && !alreadyProcessed && payment.PurchaseOrderId.HasValue)
                {
                    var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(payment.PurchaseOrderId.Value, cancellationToken);
                    if (purchaseOrder != null)
                    {
                        purchaseOrder.RecordPayment(payment.Amount);
                        purchaseOrder.ModifiedBy = currentUser;
                        await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
                        _logger.LogInformation("Updated purchase order {PONumber} paid amount by {Amount}", purchaseOrder.PONumber, payment.Amount);
                    }
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Payment {TxnNumber} confirmed. Amount: {Amount}.", payment.TransactionNumber, payment.Amount);
            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming supplier payment");
            return StatusCode(500, new { message = "An error occurred while confirming the payment" });
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound(new { message = "Supplier payment not found" });

            payment.Cancel();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling supplier payment");
            return StatusCode(500, new { message = "An error occurred while cancelling the payment" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound(new { message = "Supplier payment not found" });

            // Only allow deletion if not completed or reconciled
            if (payment.Status == "COMPLETED" || payment.IsReconciled)
                return BadRequest(new { message = "Cannot delete completed or reconciled payments" });

            await _repository.DeleteAsync(id, cancellationToken);
            return Ok(new { message = "Supplier payment deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier payment");
            return StatusCode(500, new { message = "An error occurred while deleting the payment" });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSupplierPaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound(new { message = "Supplier payment not found" });

            if (payment.Status == "CANCELLED")
                return BadRequest(new { message = "Cannot update a cancelled payment" });

            // Only non-financial reference info is editable after creation. These are set
            // unconditionally so a field can also be cleared.
            payment.SetReferenceNumber(request.ReferenceNumber);
            payment.SetAuthorizationCode(request.AuthorizationCode);
            payment.SetInvoiceNumber(request.InvoiceNumber);
            payment.UpdateNotes(request.Notes);

            payment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier payment");
            return StatusCode(500, new { message = "An error occurred while updating the payment" });
        }
    }

    [HttpPatch("{id:guid}/mark-advance")]
    public async Task<IActionResult> MarkAsAdvance(Guid id, [FromBody] MarkAsPaymentAsAdvanceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound(new { message = "Supplier payment not found" });

            // If payment was already ADVANCE, nothing to do
            if (payment.PaymentType == PaymentType.ADVANCE)
                return Ok(MapResponse(payment));

            // Block conversion for COMPLETED payments - once applied, cannot be converted
            // To "unapply" a payment, use proper reversal/refund process instead
            if (payment.Status == "COMPLETED")
            {
                return BadRequest(new { message = "Cannot convert a completed REGULAR payment to ADVANCE. The payment has already been applied to the purchase order and supplier balance. Use a reversal or refund instead." });
            }

            var currentUser = _currentUserService.GetCurrentUsername();

            payment.MarkAsAdvance();
            payment.ModifiedBy = currentUser;
            await _repository.UpdateAsync(payment, cancellationToken);

            _logger.LogInformation("Converted PENDING payment {TransactionNumber} from REGULAR to ADVANCE. Amount: {Amount}",
                payment.TransactionNumber, payment.Amount);

            return Ok(MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking supplier payment as advance");
            return StatusCode(500, new { message = "An error occurred while marking payment as advance" });
        }
    }

    [HttpPatch("{id:guid}/mark-regular")]
    public async Task<IActionResult> MarkAsRegular(Guid id, [FromBody] MarkAsPaymentAsRegularRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound(new { message = "Supplier payment not found" });

            // If payment was already REGULAR, nothing to do
            if (payment.PaymentType == PaymentType.REGULAR)
                return Ok(MapResponse(payment));

            var currentUser = _currentUserService.GetCurrentUsername();

            await using var regularTx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // If payment was ADVANCE and COMPLETED, apply the remaining amount to PO first
                if (payment.Status == "COMPLETED" && payment.PurchaseOrderId.HasValue && payment.RemainingAmount > 0)
                {
                    var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(payment.PurchaseOrderId.Value, cancellationToken);
                    if (purchaseOrder != null)
                    {
                        purchaseOrder.RecordPayment(payment.RemainingAmount);
                        purchaseOrder.ModifiedBy = currentUser;
                        await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
                        _logger.LogInformation("Applied remaining advance {Amount} to purchase order {PONumber} (converting to regular)",
                            payment.RemainingAmount, purchaseOrder.PONumber);
                    }
                }

                payment.MarkAsRegular();
                payment.ModifiedBy = currentUser;
                await _repository.UpdateAsync(payment, cancellationToken);

                await regularTx.CommitAsync(cancellationToken);
            }
            catch
            {
                await regularTx.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Converted payment {TransactionNumber} from ADVANCE to REGULAR. Amount: {Amount}",
                payment.TransactionNumber, payment.Amount);

            return Ok(MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking supplier payment as regular");
            return StatusCode(500, new { message = "An error occurred while marking payment as regular" });
        }
    }

    private static SupplierPaymentResponse MapResponse(SupplierPayment p) => new()
    {
        Id = p.Id,
        SupplierId = p.SupplierId,
        SupplierName = p.Supplier?.Name ?? "",
        PurchaseOrderId = p.PurchaseOrderId,
        GoodsReceiptId = p.GoodsReceiptId,
        PaymentProviderId = p.PaymentProviderId,
        ProviderName = p.PaymentProvider?.ProviderName ?? "",
        SupplierPaymentAccountId = p.SupplierPaymentAccountId,
        SupplierPaymentAccountName = p.SupplierPaymentAccount?.GetDisplayText() ?? "",
        TransactionNumber = p.TransactionNumber,
        Amount = p.Amount,
        PaymentFee = p.PaymentFee,
        NetAmount = p.NetAmount,
        Currency = p.Currency,
        PaymentDate = p.PaymentDate,
        PaymentMethod = p.PaymentMethod,
        Status = p.Status,
        ReferenceNumber = p.ReferenceNumber,
        AuthorizationCode = p.AuthorizationCode,
        InvoiceNumber = p.InvoiceNumber,
        Notes = p.Notes,
        ProcessedDate = p.ProcessedDate,
        ProcessedBy = p.ProcessedBy,
        ConfirmedDate = p.ConfirmedDate,
        ConfirmedBy = p.ConfirmedBy,
        IsReconciled = p.IsReconciled,
        ReconciledDate = p.ReconciledDate,
        CreatedAt = p.CreatedDate,
        PaymentType = p.PaymentType,
        Description = p.Description,
        RemainingAmount = p.RemainingAmount,
        SourceAdvancePaymentId = p.SourceAdvancePaymentId
    };

    [HttpGet("supplier/{supplierId:guid}/available-advances")]
    public async Task<IActionResult> GetAvailableAdvances(Guid supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _repository.GetBySupplierAsync(supplierId, cancellationToken);
            var availableAdvances = payments
                .Where(p => p.PaymentType == PaymentType.ADVANCE &&
                           p.Status == "COMPLETED" &&
                           p.RemainingAmount > 0)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new AvailableAdvancePayment
                {
                    Id = p.Id,
                    TransactionNumber = p.TransactionNumber,
                    Amount = p.Amount,
                    RemainingAmount = p.RemainingAmount,
                    PaymentDate = p.PaymentDate,
                    Description = p.Description
                })
                .ToList();

            return Ok(availableAdvances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available advance payments for supplier: {SupplierId}", supplierId);
            return StatusCode(500, new { message = "An error occurred while retrieving available advances" });
        }
    }

    [HttpPost("apply-advance-credit")]
    public async Task<IActionResult> ApplyAdvanceCredit(ApplyAdvanceCreditRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than 0" });

            // Get the advance payment
            var advancePayment = await _repository.GetByIdAsync(request.SourceAdvancePaymentId, cancellationToken);
            if (advancePayment == null)
                return NotFound(new { message = "Advance payment not found" });

            if (advancePayment.PaymentType != PaymentType.ADVANCE)
                return BadRequest(new { message = "Source payment is not an advance payment" });

            if (advancePayment.RemainingAmount < request.Amount)
                return BadRequest(new { message = $"Insufficient advance balance. Available: {advancePayment.RemainingAmount}, Requested: {request.Amount}" });

            // Get the purchase order
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId, cancellationToken);
            if (purchaseOrder == null)
                return NotFound(new { message = "Purchase order not found" });

            if (purchaseOrder.SupplierId != advancePayment.SupplierId)
                return BadRequest(new { message = "Purchase order supplier does not match advance payment supplier" });

            // Create new payment from advance
            var description = string.IsNullOrWhiteSpace(request.Description)
                ? $"Applied from advance payment {advancePayment.TransactionNumber}"
                : request.Description;

            var currentUser = _currentUserService.GetCurrentUsername();
            var newPayment = SupplierPayment.CreateFromAdvance(
                advancePayment.SupplierId,
                request.PurchaseOrderId,
                request.SourceAdvancePaymentId,
                advancePayment.PaymentProviderId,
                request.Amount,
                description
            );

            newPayment.CreatedBy = currentUser;
            newPayment.ModifiedBy = currentUser;

            // Reduce remaining amount on advance payment
            advancePayment.ReduceRemainingAmount(request.Amount);
            advancePayment.ModifiedBy = currentUser;

            // Update purchase order paid amount
            purchaseOrder.RecordPayment(request.Amount);
            purchaseOrder.ModifiedBy = currentUser;

            // All three writes must succeed or none should: if any one fails the ledger is inconsistent.
            await using var advanceTx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _repository.AddAsync(newPayment, cancellationToken);
                await _repository.UpdateAsync(advancePayment, cancellationToken);
                await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
                await advanceTx.CommitAsync(cancellationToken);
            }
            catch
            {
                await advanceTx.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Advance credit {Amount} applied to PO {PONumber}. Balance calculated from transactions.",
                request.Amount, purchaseOrder.PONumber);

            _logger.LogInformation(
                "Applied {Amount} from advance payment {AdvanceId} to purchase order {PONumber}. Remaining advance: {Remaining}",
                request.Amount, advancePayment.Id, purchaseOrder.PONumber, advancePayment.RemainingAmount);

            return Ok(new ApplyAdvanceCreditResponse
            {
                PaymentId = newPayment.Id,
                TransactionNumber = newPayment.TransactionNumber,
                AmountApplied = request.Amount,
                RemainingAdvanceBalance = advancePayment.RemainingAmount,
                Message = $"Successfully applied {request.Amount:C} from advance to PO {purchaseOrder.PONumber}"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when applying advance credit");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when applying advance credit");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying advance credit");
            return StatusCode(500, new { message = "An error occurred while applying advance credit" });
        }
    }
}
