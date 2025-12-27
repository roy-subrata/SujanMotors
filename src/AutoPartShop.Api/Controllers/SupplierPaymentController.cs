using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.PaymentDtos;
using AutoPartShop.Domain.Entities;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/supplier-payment")]
[ApiController]
public class SupplierPaymentController : ControllerBase
{
    private readonly ISupplierPaymentRepository _repository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly SupplierPaymentSummaryService _summaryService;
    private readonly ILogger<SupplierPaymentController> _logger;

    public SupplierPaymentController(
        ISupplierPaymentRepository repository,
        ISupplierRepository supplierRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        SupplierPaymentSummaryService summaryService,
        ILogger<SupplierPaymentController> logger)
    {
        _repository = repository;
        _supplierRepository = supplierRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _summaryService = summaryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _repository.GetAllAsync(cancellationToken);
            var responses = await Task.WhenAll(payments.Select(MapResponse));
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payments");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (allPayments, totalCount) = await _repository.GetPagedAsync(pageNumber, pageSize, cancellationToken);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var filtered = allPayments.Where(p =>
                    p.InvoiceNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.ReferenceNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                allPayments = filtered;
                totalCount = filtered.Count;
            }
            var responses = new List<SupplierPaymentResponse>();

            foreach (var payment in allPayments)
            {
                responses.Add(await MapResponse(payment));
            }

            var response = new
            {
                data = responses,
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
            _logger.LogError(ex, "Error getting supplier payments list");
            return StatusCode(500, new { message = "An error occurred while retrieving supplier payments" });
        }
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _repository.GetByStatusAsync(status, cancellationToken);
            var responses = await Task.WhenAll(payments.Select(MapResponse));
            return Ok(responses);
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
            return Ok(await MapResponse(payment));
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
            var responses = await Task.WhenAll(payments.Select(MapResponse));
            return Ok(new { data = responses, totalCount });
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
    public IActionResult GetStatusBreakdown(Guid supplierId)
    {
        try
        {
            var breakdown = _summaryService.GetStatusBreakdownAsync(supplierId);
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
            return StatusCode(500, new { message = ex.Message });
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
                        historyTable.AddCell(new Cell().Add(new Paragraph(payment.PaymentType.GetType().GetEnumValues().ToString())));
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
            var payment = SupplierPayment.Create(request.SupplierId, request.PaymentProviderId, request.Amount, request.PaymentMethod, request.TransactionNumber, request.ReferenceNumber, request.PaymentDate);
            if (request.PurchaseOrderId.HasValue)
                payment.LinkToPurchaseOrder(request.PurchaseOrderId.Value);
            if (!string.IsNullOrEmpty(request.InvoiceNumber))
                payment.SetInvoiceNumber(request.InvoiceNumber);
            payment.CreatedBy = "System";
            payment.ModifiedBy = "System";

            await _repository.AddAsync(payment, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, await MapResponse(payment));
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
    public async Task<IActionResult> MarkProcessed(Guid id, [FromBody] string processedBy, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            payment.MarkAsProcessed(processedBy);
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);

            // Update purchase order paid amount if linked
            if (payment.PurchaseOrderId.HasValue)
            {
                var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(payment.PurchaseOrderId.Value, cancellationToken);
                if (purchaseOrder != null)
                {
                    purchaseOrder.RecordPayment(payment.Amount);
                    purchaseOrder.ModifiedBy = "System";
                    await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
                }
            }

            return Ok(await MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking supplier payment as processed");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(Guid id, [FromBody] string confirmedBy, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();
            payment.ConfirmReceipt(confirmedBy);
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
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
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
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

            // Mark as processed and confirmed
            payment.MarkAsProcessed("System");
            payment.ConfirmReceipt("System");
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);

            // Update purchase order paid amount if linked
            if (payment.PurchaseOrderId.HasValue)
            {
                var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(payment.PurchaseOrderId.Value, cancellationToken);
                if (purchaseOrder != null)
                {
                    purchaseOrder.RecordPayment(payment.Amount);
                    purchaseOrder.ModifiedBy = "System";
                    await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
                }
            }

            return Ok(await MapResponse(payment));
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
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
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

            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
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

            payment.MarkAsAdvance();
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
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

            payment.MarkAsRegular();
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking supplier payment as regular");
            return StatusCode(500, new { message = "An error occurred while marking payment as regular" });
        }
    }

    private async Task<SupplierPaymentResponse> MapResponse(SupplierPayment p)
    {
        var supplier = await _supplierRepository.GetByIdAsync(p.SupplierId);
        return new()
        {
            Id = p.Id,
            SupplierId = p.SupplierId,
            SupplierName = supplier?.Name ?? "",
            PurchaseOrderId = p.PurchaseOrderId,
            PaymentProviderId = p.PaymentProviderId,
            ProviderName = "",
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
            CreatedAt = DateTime.UtcNow,
            PaymentType = p.PaymentType,
            Description = p.Description
        };
    }
}
