using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.PaymentDtos;
using AutoPartShop.Application.Supplier;
using AutoPartShop.Application.SupplierPayment.Dtos;
using AutoPartShop.Domain.Entities;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/supplier-payments")]
[Route("api/v1/supplier-payments")]
[ApiController]
[Authorize]
public class SupplierPaymentController : ControllerBase
{
    private readonly ISupplierPaymentRepository _repository;
    public readonly ISupplierPaymentReadRespository _supplierPaymentReadRespository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly SupplierPaymentSummaryService _summaryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SupplierPaymentController> _logger;

    public SupplierPaymentController(
        ISupplierPaymentRepository repository,
        ISupplierPaymentReadRespository supplierPaymentReadRespository,
        IPurchaseOrderRepository purchaseOrderRepository,
        SupplierPaymentSummaryService summaryService,
        ICurrentUserService currentUserService,
        ILogger<SupplierPaymentController> logger)
    {
        _repository = repository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _summaryService = summaryService;
        _currentUserService = currentUserService;
        _supplierPaymentReadRespository = supplierPaymentReadRespository;
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
    public async Task<IActionResult> DownloadPaymentSummaryReport(Guid supplierId, CancellationToken cancellationToken = default)
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

            var pdfBytes = GeneratePdfReport(summary);
            _logger.LogInformation("PDF report generated, size: {Size}", pdfBytes.Length);

            return File(pdfBytes, "application/pdf", $"payment-summary-{summary.SupplierCode}-{DateTime.UtcNow:yyyyMMdd}.pdf");
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

    private byte[] GeneratePdfReport(SupplierPaymentHistorySummary summary)
    {
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                var pdfWriter = new PdfWriter(memoryStream);
                var pdfDocument = new PdfDocument(pdfWriter);
                var document = new Document(pdfDocument);
                document.SetMargins(20, 20, 20, 20);

                // Title
                var title = new Paragraph("SUPPLIER PAYMENT SUMMARY REPORT")
                    .SetFontSize(16)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10);
                document.Add(title);

                // Generation Date
                var generatedDate = new Paragraph($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(generatedDate);

                // Supplier Information
                document.Add(new Paragraph("SUPPLIER INFORMATION").SetBold().SetFontSize(12).SetMarginTop(10).SetMarginBottom(5));
                var supplierTable = new Table(2).SetWidth(100);
                supplierTable.AddCell(new Cell().Add(new Paragraph("Supplier Name:")).SetBold());
                supplierTable.AddCell(new Cell().Add(new Paragraph(summary.SupplierName)));
                supplierTable.AddCell(new Cell().Add(new Paragraph("Supplier Code:")).SetBold());
                supplierTable.AddCell(new Cell().Add(new Paragraph(summary.SupplierCode)));
                document.Add(supplierTable);

                // Key Metrics
                document.Add(new Paragraph("KEY METRICS").SetBold().SetFontSize(12).SetMarginTop(15).SetMarginBottom(5));
                var metricsTable = new Table(2).SetWidth(100);
                metricsTable.AddCell(new Cell().Add(new Paragraph("Total Paid:")).SetBold());
                metricsTable.AddCell(new Cell().Add(new Paragraph($"₹ {summary.TotalPaid:N2}")));
                metricsTable.AddCell(new Cell().Add(new Paragraph("Total Due:")).SetBold());
                metricsTable.AddCell(new Cell().Add(new Paragraph($"₹ {summary.TotalDue:N2}")));
                metricsTable.AddCell(new Cell().Add(new Paragraph("Total Advance:")).SetBold());
                metricsTable.AddCell(new Cell().Add(new Paragraph($"₹ {summary.TotalAdvanceAmount:N2}")));
                metricsTable.AddCell(new Cell().Add(new Paragraph("Payment Balance:")).SetBold());
                metricsTable.AddCell(new Cell().Add(new Paragraph($"₹ {summary.PaymentBalance:N2}")));
                metricsTable.AddCell(new Cell().Add(new Paragraph("Total Fees:")).SetBold());
                metricsTable.AddCell(new Cell().Add(new Paragraph($"₹ {summary.TotalFees:N2}")));
                document.Add(metricsTable);

                // Credit Information
                document.Add(new Paragraph("CREDIT INFORMATION").SetBold().SetFontSize(12).SetMarginTop(15).SetMarginBottom(5));
                var creditTable = new Table(2).SetWidth(100);
                creditTable.AddCell(new Cell().Add(new Paragraph("Credit Limit:")).SetBold());
                creditTable.AddCell(new Cell().Add(new Paragraph($"₹ {summary.CreditLimit:N2}")));
                creditTable.AddCell(new Cell().Add(new Paragraph("Credit Utilized:")).SetBold());
                creditTable.AddCell(new Cell().Add(new Paragraph($"{summary.CreditUtilization:N2}%")));
                creditTable.AddCell(new Cell().Add(new Paragraph("Outstanding Invoices:")).SetBold());
                creditTable.AddCell(new Cell().Add(new Paragraph(summary.OutstandingInvoiceCount.ToString())));
                document.Add(creditTable);

                // Payment Status Breakdown
                document.Add(new Paragraph("PAYMENT STATUS BREAKDOWN").SetBold().SetFontSize(12).SetMarginTop(15).SetMarginBottom(5));
                var statusTable = new Table(2).SetWidth(100);
                statusTable.AddCell(new Cell().Add(new Paragraph("Completed Payments:")).SetBold());
                statusTable.AddCell(new Cell().Add(new Paragraph(summary.CompletedPayments.ToString())));
                statusTable.AddCell(new Cell().Add(new Paragraph("Pending Payments:")).SetBold());
                statusTable.AddCell(new Cell().Add(new Paragraph(summary.PendingPayments.ToString())));
                statusTable.AddCell(new Cell().Add(new Paragraph("Processing Payments:")).SetBold());
                statusTable.AddCell(new Cell().Add(new Paragraph(summary.ProcessingPayments.ToString())));
                statusTable.AddCell(new Cell().Add(new Paragraph("Failed Payments:")).SetBold());
                statusTable.AddCell(new Cell().Add(new Paragraph(summary.FailedPayments.ToString())));
                statusTable.AddCell(new Cell().Add(new Paragraph("Cancelled Payments:")).SetBold());
                statusTable.AddCell(new Cell().Add(new Paragraph(summary.CancelledPayments.ToString())));
                document.Add(statusTable);

                // Last Payment
                if (summary.LastPaymentDate.HasValue)
                {
                    document.Add(new Paragraph("LAST PAYMENT").SetBold().SetFontSize(12).SetMarginTop(15).SetMarginBottom(5));
                    var lastPaymentTable = new Table(2).SetWidth(100);
                    lastPaymentTable.AddCell(new Cell().Add(new Paragraph("Date:")).SetBold());
                    lastPaymentTable.AddCell(new Cell().Add(new Paragraph(summary.LastPaymentDate?.ToString("yyyy-MM-dd"))));
                    lastPaymentTable.AddCell(new Cell().Add(new Paragraph("Amount:")).SetBold());
                    lastPaymentTable.AddCell(new Cell().Add(new Paragraph($"₹ {summary.LastPaymentAmount:N2}")));
                    document.Add(lastPaymentTable);
                }

                // Payment History
                if (summary.PaymentHistory?.Count > 0)
                {
                    document.Add(new Paragraph("PAYMENT HISTORY").SetBold().SetFontSize(12).SetMarginTop(15).SetMarginBottom(5));
                    var historyTable = new Table(6).SetWidth(100);

                    // Header row
                    historyTable.AddHeaderCell(new Cell().Add(new Paragraph("Date").SetBold()));
                    historyTable.AddHeaderCell(new Cell().Add(new Paragraph("Amount").SetBold()));
                    historyTable.AddHeaderCell(new Cell().Add(new Paragraph("Type").SetBold()));
                    historyTable.AddHeaderCell(new Cell().Add(new Paragraph("Method").SetBold()));
                    historyTable.AddHeaderCell(new Cell().Add(new Paragraph("Invoice").SetBold()));
                    historyTable.AddHeaderCell(new Cell().Add(new Paragraph("Status").SetBold()));

                    // Data rows
                    foreach (var payment in summary.PaymentHistory)
                    {
                        historyTable.AddCell(new Cell().Add(new Paragraph(payment.PaymentDate.ToString())));
                        historyTable.AddCell(new Cell().Add(new Paragraph($"₹ {payment.Amount:N2}")));
                        historyTable.AddCell(new Cell().Add(new Paragraph(payment.PaymentType.ToString())));
                        historyTable.AddCell(new Cell().Add(new Paragraph(payment.PaymentMethod)));
                        historyTable.AddCell(new Cell().Add(new Paragraph(payment.InvoiceNumber)));
                        historyTable.AddCell(new Cell().Add(new Paragraph(payment.Status)));
                    }

                    document.Add(historyTable);
                }

                // Footer
                document.Add(new Paragraph("\nThis report was automatically generated by AutoPartShop.")
                    .SetFontSize(9)
                    .SetItalic()
                    .SetMarginTop(20));

                document.Close();
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF report");
            throw;
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

            // Auto-confirm CASH payments since money is immediately in hand
            if (request.PaymentMethod?.Equals("CASH", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Auto-confirming CASH payment of {Amount} for supplier {SupplierId}, Type: {PaymentType}",
                    request.Amount, request.SupplierId, request.PaymentType);
                payment.MarkAsProcessed(currentUser + " - Auto-confirmed");
                payment.ConfirmReceipt(currentUser + " - Cash Payment");

                // For REGULAR payments: Update purchase order and supplier balance
                if (request.PaymentType == PaymentType.REGULAR)
                {
                    // Update purchase order paid amount (required for REGULAR)
                    var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId!.Value, cancellationToken);
                    if (purchaseOrder != null)
                    {
                        purchaseOrder.RecordPayment(payment.Amount);
                        purchaseOrder.ModifiedBy = currentUser;
                        await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
                        _logger.LogInformation("Updated purchase order {PONumber} paid amount by {Amount}", purchaseOrder.PONumber, payment.Amount);
                    }

                    // NOTE: Supplier balance is NOT updated here.
                    // Balance is now calculated from transactions via SupplierLedgerService.
                    _logger.LogInformation("Payment {TxnNumber} completed for supplier. Amount: {Amount}. Balance calculated from transactions.",
                        payment.TransactionNumber, payment.Amount);
                }
                // For ADVANCE payments: Balance is calculated from transactions
                // Advances contribute to available credit, not direct balance reduction
                else
                {
                    _logger.LogInformation("ADVANCE payment created. Supplier balance NOT updated - will be applied when used for a PO.");
                }
            }

            await _repository.AddAsync(payment, cancellationToken);
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
            await _repository.UpdateAsync(payment, cancellationToken);

            // Only update PO and supplier balance for REGULAR payments
            // ADVANCE payments don't affect balance until they're applied to a PO
            if (payment.PaymentType == PaymentType.REGULAR)
            {
                // Update purchase order paid amount if linked
                if (payment.PurchaseOrderId.HasValue)
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

                // NOTE: Supplier balance is NOT updated here.
                // Balance is now calculated from transactions via SupplierLedgerService.
                _logger.LogInformation("Payment {TxnNumber} marked as processed. Amount: {Amount}. Balance calculated from transactions.",
                    payment.TransactionNumber, payment.Amount);
            }
            else
            {
                _logger.LogInformation("ADVANCE payment {TransactionNumber} processed. Balance calculated from transactions.",
                    payment.TransactionNumber);
            }

            return Ok(MapResponse(payment));
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
            await _repository.UpdateAsync(payment, cancellationToken);

            // Only update PO for REGULAR payments that have NOT yet had mark-processed called
            // (mark-processed already records the payment on the PO; calling it again would double-count)
            if (payment.PaymentType == PaymentType.REGULAR && !alreadyProcessed)
            {
                if (payment.PurchaseOrderId.HasValue)
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

                _logger.LogInformation("Payment {TxnNumber} confirmed. Amount: {Amount}. Balance calculated from transactions.",
                    payment.TransactionNumber, payment.Amount);
            }
            else
            {
                _logger.LogInformation("Payment {TransactionNumber} confirmed (PO already updated). Balance calculated from transactions.",
                    payment.TransactionNumber);
            }

            return Ok(MapResponse(payment));
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

            // Only allow updates if not confirmed or reconciled
            if (payment.Status == "COMPLETED" || payment.Status == "RECONCILED")
                return BadRequest(new { message = "Cannot update completed or reconciled payments" });

            // Update mutable fields
            if (!string.IsNullOrEmpty(request.AuthorizationCode))
                payment.SetAuthorizationCode(request.AuthorizationCode);

            if (!string.IsNullOrEmpty(request.Notes))
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

            // If payment was ADVANCE and COMPLETED, we need to apply the remaining amount
            // because converting to REGULAR means the money should be applied to PO/supplier
            if (payment.Status == "COMPLETED")
            {
                // Apply the remaining advance amount to purchase order if linked
                if (payment.PurchaseOrderId.HasValue && payment.RemainingAmount > 0)
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

                // NOTE: Supplier balance is NOT updated here.
                // Balance is now calculated from transactions via SupplierLedgerService.
                if (payment.RemainingAmount > 0)
                {
                    _logger.LogInformation("Advance payment {TxnNumber} converted to regular. Remaining amount: {Amount}. Balance calculated from transactions.",
                        payment.TransactionNumber, payment.RemainingAmount);
                }
            }

            payment.MarkAsRegular();
            payment.ModifiedBy = currentUser;
            await _repository.UpdateAsync(payment, cancellationToken);

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

            // NOTE: Supplier balance is NOT updated here.
            // Balance is now calculated from transactions via SupplierLedgerService.
            _logger.LogInformation("Advance credit {Amount} applied to PO {PONumber}. Balance calculated from transactions.",
                request.Amount, purchaseOrder.PONumber);

            // Save all changes
            await _repository.AddAsync(newPayment, cancellationToken);
            await _repository.UpdateAsync(advancePayment, cancellationToken);
            await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);

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
