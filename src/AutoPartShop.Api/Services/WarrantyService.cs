using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

public interface IWarrantyService
{
    Task<string> GenerateWarrantyNumberAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateClaimNumberAsync(CancellationToken cancellationToken = default);
    Task<WarrantyRegistration> CreateWarrantyForSalesOrderLineAsync(
        SalesOrderLine salesOrderLine,
        Guid salesOrderId,
        Guid customerId,
        DateTime saleDate,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a warranty claim. Handles REPLACEMENT stock movements and REFUND
    /// payments/credit notes atomically within a database transaction.
    /// Throws <see cref="InvalidOperationException"/> for business-rule violations.
    /// </summary>
    Task<WarrantyClaim> CompleteClaimAsync(
        Guid claimId,
        string resolutionDetails,
        string? refundType,
        decimal? refundAmount,
        string? referenceNumber,
        string? refundNotes,
        bool returnItemReceived,
        bool restockAsSellable,
        bool replacementFromVendor,
        string actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a warranty claim and reactivates the warranty when no other active
    /// claims remain. Both updates happen atomically.
    /// </summary>
    Task<WarrantyClaim> RejectClaimAsync(
        Guid claimId,
        string rejectionReason,
        string rejectedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a warranty claim. For REPAIR claims, reactivates the warranty when no
    /// other active claims remain. Both updates happen atomically.
    /// </summary>
    Task<WarrantyClaim> CloseClaimAsync(
        Guid claimId,
        string? closureNotes,
        CancellationToken cancellationToken = default);
}

public class WarrantyService(
    ICodeGenerateService codeGenerateService,
    IProductRepository productRepository,
    IWarrantyRegistrationRepository warrantyRepository,
    IWarrantyClaimRepository claimRepository,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    ISalesOrderRepository salesOrderRepository,
    ICustomerRepository customerRepository,
    ICustomerPaymentRepository customerPaymentRepository,
    ICustomerCreditNoteRepository customerCreditNoteRepository,
    AutoPartDbContext dbContext) : IWarrantyService
{
    private const string WarrantyReplacementOutReason = "WARRANTY_REPLACEMENT_OUT";
    private const string WarrantyDefectiveReturnReason = "WARRANTY_DEFECTIVE_RETURN";
    private const string WarrantyRefundReturnReason = "WARRANTY_REFUND_RETURN";

    public async Task<string> GenerateWarrantyNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"WR-{year}-";

        // Handle legacy data where existing numbers may already occupy early sequence values.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var generated = await codeGenerateService.GenerateAsync(prefix, cancellationToken, minDigits: 5);
            var exists = await warrantyRepository.GetByWarrantyNumberAsync(generated, cancellationToken);
            if (exists == null)
                return generated;
        }

        throw new InvalidOperationException("Unable to generate a unique warranty number. Please try again.");
    }

    public async Task<string> GenerateClaimNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"WC-{year}-";

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var generated = await codeGenerateService.GenerateAsync(prefix, cancellationToken, minDigits: 5);
            var exists = await claimRepository.ClaimNumberExistsAsync(generated, cancellationToken);
            if (!exists)
                return generated;
        }

        throw new InvalidOperationException("Unable to generate a unique claim number. Please try again.");
    }

    public async Task<WarrantyRegistration> CreateWarrantyForSalesOrderLineAsync(
        SalesOrderLine salesOrderLine,
        Guid salesOrderId,
        Guid customerId,
        DateTime saleDate,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var part = await productRepository.GetByIdAsync(salesOrderLine.PartId, cancellationToken);
        if (part == null)
            throw new InvalidOperationException($"Part not found: {salesOrderLine.PartId}");

        // Load variant when present — its warranty override takes highest priority
        ProductVariant? variant = null;
        if (salesOrderLine.ProductVariantId.HasValue)
            variant = await dbContext.Set<ProductVariant>()
                .FirstOrDefaultAsync(v => v.Id == salesOrderLine.ProductVariantId.Value, cancellationToken);

        // Priority: variant override → lot override → part master
        bool hasWarranty;
        int warrantyPeriodMonths;
        string warrantyType;
        string warrantyTerms;
        string certificateTemplate;

        if (variant != null && variant.HasWarrantyOverride.HasValue)
        {
            // Variant override is authoritative — ignore lot and part warranty
            (hasWarranty, var vPeriod, var vType) = variant.ResolveWarranty(part);
            warrantyPeriodMonths = vPeriod ?? 0;
            warrantyType = vType ?? "SELLER";
            warrantyTerms = part.WarrantyTerms ?? "Standard warranty terms apply";
        }
        else if (warehouseId != Guid.Empty)
        {
            var fifoLot = await dbContext.StockLots
                .Where(sl => sl.PartId == salesOrderLine.PartId &&
                             sl.WarehouseId == warehouseId &&
                             !sl.Isdeleted)
                .OrderBy(sl => sl.ExpiryDate == null ? 1 : 0)
                .ThenBy(sl => sl.ExpiryDate)
                .ThenBy(sl => sl.ReceivingDate)
                .ThenBy(sl => sl.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            hasWarranty = fifoLot?.HasWarranty ?? part.HasWarranty;
            warrantyPeriodMonths = (fifoLot?.HasWarranty == true ? fifoLot.WarrantyPeriodMonths : null)
                                   ?? part.WarrantyPeriodMonths ?? 0;
            warrantyType = (fifoLot?.HasWarranty == true ? fifoLot.WarrantyType : null)
                           ?? part.WarrantyType ?? "SELLER";
            warrantyTerms = (fifoLot?.HasWarranty == true ? fifoLot.WarrantyTerms : null)
                            ?? part.WarrantyTerms ?? "Standard warranty terms apply";
        }
        else
        {
            hasWarranty = part.HasWarranty;
            warrantyPeriodMonths = part.WarrantyPeriodMonths ?? 0;
            warrantyType = part.WarrantyType ?? "SELLER";
            warrantyTerms = part.WarrantyTerms ?? "Standard warranty terms apply";
        }

        certificateTemplate = part.WarrantyCertificateTemplate ?? string.Empty;

        if (!hasWarranty || warrantyPeriodMonths <= 0)
            throw new InvalidOperationException($"Part {part.Name} does not have warranty for this lot");

        var warrantyNumber = await GenerateWarrantyNumberAsync(cancellationToken);
        var certificateNumber = string.IsNullOrWhiteSpace(certificateTemplate)
            ? $"CERT-{warrantyNumber}"
            : $"{certificateTemplate}-{warrantyNumber}";

        var warranty = WarrantyRegistration.Create(
            warrantyNumber: warrantyNumber,
            partId: part.Id,
            salesOrderId: salesOrderId,
            salesOrderLineId: salesOrderLine.Id,
            customerId: customerId,
            saleDate: saleDate,
            warrantyStartDate: saleDate,
            warrantyPeriodMonths: warrantyPeriodMonths,
            warrantyType: warrantyType,
            warrantyTerms: warrantyTerms,
            certificateNumber: certificateNumber,
            productVariantId: salesOrderLine.ProductVariantId
        );

        await warrantyRepository.AddAsync(warranty, cancellationToken);
        return warranty;
    }

    public async Task<WarrantyClaim> CompleteClaimAsync(
        Guid claimId,
        string resolutionDetails,
        string? refundType,
        decimal? refundAmount,
        string? referenceNumber,
        string? refundNotes,
        bool returnItemReceived,
        bool restockAsSellable,
        bool replacementFromVendor,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(claimId, cancellationToken)
            ?? throw new InvalidOperationException("Warranty claim not found");

        // For replacement/refund, the claim can be completed directly from APPROVED.
        if (claim.Status == "APPROVED" &&
            (claim.ServiceType.Equals("REPLACEMENT", StringComparison.OrdinalIgnoreCase) ||
             claim.ServiceType.Equals("REFUND", StringComparison.OrdinalIgnoreCase)))
        {
            claim.StartServiceWithoutTechnician();
        }

        // EnableRetryOnFailure is configured globally, so a manual transaction must run inside an
        // execution strategy that owns the whole unit (otherwise EF throws on BeginTransaction).
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (claim.ServiceType.Equals("REPLACEMENT", StringComparison.OrdinalIgnoreCase))
                    await ProcessReplacementAsync(claim, returnItemReceived, replacementFromVendor, cancellationToken);

                if (claim.ServiceType.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
                    await ProcessRefundAsync(claim, refundType, refundAmount, referenceNumber, refundNotes,
                        returnItemReceived, restockAsSellable, actor, cancellationToken);

                claim.Complete(resolutionDetails);
                await claimRepository.UpdateAsync(claim, cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return claim;
    }

    public async Task<WarrantyClaim> RejectClaimAsync(
        Guid claimId,
        string rejectionReason,
        string rejectedBy,
        CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(claimId, cancellationToken)
            ?? throw new InvalidOperationException("Warranty claim not found");

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                claim.Reject(rejectionReason, rejectedBy);
                await claimRepository.UpdateAsync(claim, cancellationToken);

                // Reactivate the warranty if no other active claim remains,
                // so the customer can file a new claim on a still-valid warranty.
                var warranty = await warrantyRepository.GetByIdAsync(claim.WarrantyRegistrationId, cancellationToken);
                if (warranty != null && warranty.Status == "CLAIMED")
                {
                    var activeStatuses = new[] { "PENDING", "UNDER_REVIEW", "APPROVED", "IN_PROGRESS" };
                    var allClaims = await claimRepository.GetByWarrantyRegistrationIdAsync(
                        claim.WarrantyRegistrationId, cancellationToken);
                    var hasOtherActiveClaims = allClaims.Any(c =>
                        c.Id != claim.Id &&
                        activeStatuses.Contains(c.Status, StringComparer.OrdinalIgnoreCase));

                    if (!hasOtherActiveClaims)
                    {
                        warranty.ReactivateAfterClaimRejection();
                        await warrantyRepository.UpdateAsync(warranty, cancellationToken);
                    }
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return claim;
    }

    public async Task<WarrantyClaim> CloseClaimAsync(
        Guid claimId,
        string? closureNotes,
        CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(claimId, cancellationToken)
            ?? throw new InvalidOperationException("Warranty claim not found");

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                claim.Close(closureNotes);
                await claimRepository.UpdateAsync(claim, cancellationToken);

                // REPAIR is non-terminal — reactivate warranty so customer can file again.
                // REPLACEMENT and REFUND consume the warranty; it stays CLAIMED.
                if (claim.ServiceType.Equals("REPAIR", StringComparison.OrdinalIgnoreCase))
                {
                    var warranty = await warrantyRepository.GetByIdAsync(claim.WarrantyRegistrationId, cancellationToken);
                    if (warranty != null && warranty.Status == "CLAIMED")
                    {
                        var activeStatuses = new[] { "PENDING", "UNDER_REVIEW", "APPROVED", "IN_PROGRESS" };
                        var allClaims = await claimRepository.GetByWarrantyRegistrationIdAsync(
                            claim.WarrantyRegistrationId, cancellationToken);
                        var hasOtherActiveClaims = allClaims.Any(c =>
                            c.Id != claim.Id &&
                            activeStatuses.Contains(c.Status, StringComparer.OrdinalIgnoreCase));

                        if (!hasOtherActiveClaims)
                        {
                            warranty.ReactivateAfterClaimClosure();
                            await warrantyRepository.UpdateAsync(warranty, cancellationToken);
                        }
                    }
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return claim;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task ProcessReplacementAsync(
        WarrantyClaim claim,
        bool returnItemReceived,
        bool replacementFromVendor,
        CancellationToken ct)
    {
        var warranty = await warrantyRepository.GetByIdAsync(claim.WarrantyRegistrationId, ct);
        if (warranty == null)
            throw new InvalidOperationException("Warranty registration not found for replacement processing");

        // Variant-aware: only touch the stock of the exact variant that was sold.
        var stockLevels = (await stockLevelRepository.GetByPartAndVariantAsync(
                warranty.PartId, warranty.ProductVariantId, ct))
            .Where(s => s.IsActive)
            .ToList();

        StockLevel? dispatchedFrom = null;

        if (!replacementFromVendor)
        {
            // OUT: replacement part dispatched to customer from on-hand stock now.
            var sellable = stockLevels
                .Where(s => s.QuantityAvailable > 0)
                .OrderByDescending(s => s.QuantityAvailable)
                .FirstOrDefault();

            if (sellable == null)
                throw new InvalidOperationException(
                    "No available replacement stock found. Add stock first, or mark the replacement as vendor-sourced, then complete the claim.");

            dispatchedFrom = sellable;
            dispatchedFrom.RemoveStock(1);
            await stockLevelRepository.UpdateAsync(dispatchedFrom, ct);

            var outMovement = StockMovement.Create(
                stockLevelId: dispatchedFrom.Id,
                movementType: "OUT",
                quantity: 1,
                reason: WarrantyReplacementOutReason,
                referenceNumber: claim.ClaimNumber);
            outMovement.Approve("system");
            outMovement.AddNotes($"Replacement dispatched for warranty claim {claim.ClaimNumber}");
            await stockMovementRepository.AddAsync(outMovement, ct);
        }

        if (returnItemReceived)
        {
            // IN: defective item returned by customer, quarantined so it cannot be resold.
            // Prefer the location the replacement came from; otherwise any active location for the variant.
            var quarantineAt = dispatchedFrom
                ?? stockLevels.OrderByDescending(s => s.QuantityOnHand).FirstOrDefault();

            if (quarantineAt == null)
                throw new InvalidOperationException(
                    "No stock location found to receive the defective item. Create stock for this part/variant first, then complete the claim.");

            quarantineAt.AddStock(1);
            quarantineAt.ReserveStock(1);
            await stockLevelRepository.UpdateAsync(quarantineAt, ct);

            var inMovement = StockMovement.Create(
                stockLevelId: quarantineAt.Id,
                movementType: "IN",
                quantity: 1,
                reason: WarrantyDefectiveReturnReason,
                referenceNumber: claim.ClaimNumber);
            inMovement.Approve("system");
            inMovement.AddNotes(
                $"Defective item returned and quarantined under warranty claim {claim.ClaimNumber}. Reserved from sale.");
            await stockMovementRepository.AddAsync(inMovement, ct);
        }
    }

    private async Task ProcessRefundAsync(
        WarrantyClaim claim,
        string? refundType,
        decimal? refundAmount,
        string? referenceNumber,
        string? refundNotes,
        bool returnItemReceived,
        bool restockAsSellable,
        string actor,
        CancellationToken ct)
    {
        var warranty = await warrantyRepository.GetByIdAsync(claim.WarrantyRegistrationId, ct)
            ?? throw new InvalidOperationException("Warranty registration not found for refund processing");

        if (returnItemReceived)
        {
            // Variant-aware: return the unit to the exact variant's stock, not just any level for the part.
            var stockLevels = (await stockLevelRepository.GetByPartAndVariantAsync(
                    warranty.PartId, warranty.ProductVariantId, ct))
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.QuantityOnHand)
                .ToList();

            if (!stockLevels.Any())
                throw new InvalidOperationException(
                    "No stock location found for returned refund item. Create stock for this part first, then complete the refund claim.");

            var stockLevel = stockLevels.First();
            stockLevel.AddStock(1);
            if (!restockAsSellable)
                stockLevel.ReserveStock(1);
            await stockLevelRepository.UpdateAsync(stockLevel, ct);

            var returnMovement = StockMovement.Create(
                stockLevelId: stockLevel.Id,
                movementType: "IN",
                quantity: 1,
                reason: WarrantyRefundReturnReason,
                referenceNumber: claim.ClaimNumber);
            returnMovement.Approve("system");
            returnMovement.AddNotes(restockAsSellable
                ? $"Refund item returned for warranty claim {claim.ClaimNumber} and added back to sellable stock."
                : $"Refund item returned for warranty claim {claim.ClaimNumber} and quarantined from sale.");
            await stockMovementRepository.AddAsync(returnMovement, ct);
        }

        var salesOrder = await salesOrderRepository.GetByIdAsync(warranty.SalesOrderId, ct)
            ?? throw new InvalidOperationException("Sales order not found for refund processing");

        var orderLine = salesOrder.LineItems.FirstOrDefault(x => x.Id == warranty.SalesOrderLineId);
        var fallbackAmount = orderLine != null ? Math.Max(0, orderLine.UnitPrice - orderLine.Discount) : 0;
        var effectiveRefundAmount = refundAmount.HasValue && refundAmount.Value > 0
            ? refundAmount.Value
            : fallbackAmount;

        if (effectiveRefundAmount <= 0)
            throw new InvalidOperationException(
                "Refund amount is required for REFUND claims. Provide RefundAmount or ensure original sale amount exists.");

        // Cap the refund at the value of the returned item, regardless of refund type, so a
        // warranty refund can never exceed what the line was originally sold for.
        if (orderLine != null && effectiveRefundAmount > fallbackAmount)
            throw new InvalidOperationException(
                $"Refund amount ({effectiveRefundAmount}) cannot exceed the value of the returned item ({fallbackAmount}).");

        var normalizedRefundType = string.IsNullOrWhiteSpace(refundType)
            ? "CASH_REFUND"
            : refundType.Trim().ToUpperInvariant();

        if (normalizedRefundType != "CASH_REFUND" && normalizedRefundType != "STORE_CREDIT")
            throw new InvalidOperationException("RefundType must be CASH_REFUND or STORE_CREDIT");

        if (normalizedRefundType == "CASH_REFUND")
        {
            if (effectiveRefundAmount > salesOrder.PaidAmount)
                throw new InvalidOperationException(
                    $"Cash refund ({effectiveRefundAmount}) cannot exceed the amount the customer actually paid ({salesOrder.PaidAmount}). Use STORE_CREDIT, or refund up to the paid amount.");

            var customer = await customerRepository.GetByIdAsync(claim.CustomerId, ct)
                ?? throw new InvalidOperationException("Customer not found for refund processing");

            customer.UpdateBalance(-effectiveRefundAmount);
            customer.ModifiedBy = actor;
            await customerRepository.UpdateAsync(customer, ct);

            var refundPayment = CustomerPayment.Create(
                customerId: claim.CustomerId,
                paymentProviderId: null,
                amount: -effectiveRefundAmount,
                paymentMethod: "REFUND",
                transactionNumber: $"WREFUND-{claim.ClaimNumber}",
                referenceNumber: referenceNumber ?? claim.ClaimNumber,
                paymentDate: DateTime.UtcNow);

            refundPayment.LinkToWarrantyClaim(claim.Id);
            refundPayment.MarkAsCompleted();
            refundPayment.CreatedBy = actor;
            refundPayment.ModifiedBy = actor;
            refundPayment.UpdateNotes($"Warranty cash refund for claim {claim.ClaimNumber}. {refundNotes}".Trim());
            await customerPaymentRepository.AddAsync(refundPayment, ct);

            salesOrder.ProcessRefund(effectiveRefundAmount);
            salesOrder.ModifiedBy = actor;
            await salesOrderRepository.UpdateAsync(salesOrder, ct);
        }
        else
        {
            var creditNote = CustomerCreditNote.Create(
                creditNoteNumber: $"CN-WAR-{claim.ClaimNumber}",
                customerId: claim.CustomerId,
                salesReturnId: null,
                amount: effectiveRefundAmount,
                currency: salesOrder.Currency,
                issueDate: DateTime.UtcNow,
                expiryDate: DateTime.UtcNow.AddMonths(6),
                notes: $"Warranty store credit for claim {claim.ClaimNumber}. {refundNotes}".Trim(),
                issuedBy: actor);

            creditNote.LinkToWarrantyClaim(claim.Id);
            await customerCreditNoteRepository.AddAsync(creditNote, ct);
        }
    }
}
