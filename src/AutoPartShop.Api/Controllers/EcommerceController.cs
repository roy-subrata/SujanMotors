using System.Security.Claims;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/ecommerce")]
[Route("api/v1/ecommerce")]
[ApiController]
[Produces("application/json")]
public class EcommerceController(
    AutoPartDbContext _dbContext,
    ICustomerRepository _customerRepository,
    ICustomerVehicleRepository _customerVehicleRepository,
    ISalesOrderRepository _salesOrderRepository,
    IInvoiceRepository _invoiceRepository,
    ICustomerPaymentRepository _customerPaymentRepository,
    ICodeGenerateService _codeGenerateService,
    ICurrentUserService _currentUserService,
    ILogger<EcommerceController> _logger) : ControllerBase
{
    // ── Promo Code Validation (public — called before checkout) ─────────────

    [HttpGet("promo/validate")]
    public async Task<IActionResult> ValidatePromoCode(
        [FromQuery] string code,
        [FromQuery] decimal cartTotal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { valid = false, message = "Code is required" });

        var today = DateTime.UtcNow.Date;
        var discount = await _dbContext.Discounts
            .FirstOrDefaultAsync(d => d.IsActive && !d.Isdeleted
                && d.PromoCode != null
                && d.PromoCode.ToUpper() == code.Trim().ToUpper()
                && d.PartId == null
                && d.StartDate.Date <= today
                && (!d.EndDate.HasValue || d.EndDate.Value.Date >= today),
                cancellationToken);

        if (discount == null)
            return Ok(new { valid = false, message = "Invalid or expired promo code" });

        if (discount.MinimumCartAmount.HasValue && cartTotal < discount.MinimumCartAmount.Value)
            return Ok(new
            {
                valid = false,
                message = $"This code requires a minimum cart total of {discount.MinimumCartAmount.Value:N0}"
            });

        var discountAmount = discount.CalculateDiscountAmount(cartTotal);

        return Ok(new
        {
            valid = true,
            code = discount.PromoCode,
            discountType = discount.Type,
            discountValue = discount.Value,
            discountAmount,
            finalTotal = Math.Max(0, cartTotal - discountAmount),
            description = discount.Description ?? discount.Name,
            minimumCartAmount = discount.MinimumCartAmount
        });
    }

    [HttpPost("checkout")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Checkout([FromBody] EcommerceCheckoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "Cart is empty" });

            // 1. Load the authenticated customer from JWT claim
            var customerIdClaim = User.FindFirstValue("customerId");
            if (!Guid.TryParse(customerIdClaim, out var customerId))
                return Unauthorized(new { message = "Invalid token" });

            var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
            if (customer == null)
                return NotFound(new { message = "Customer account not found" });

            var isNewCustomer = false;

            // 2. Get default warehouse (first active one)
            var warehouse = await _dbContext.Warehouses
                .Where(w => w.IsActive && !w.Isdeleted)
                .OrderBy(w => w.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (warehouse == null)
                return BadRequest(new { message = "No active warehouse is configured. Please contact the administrator." });

            // 3. Validate parts and resolve prices
            var lineRequests = new List<(Guid partId, Guid? variantId, int qty, decimal price)>();
            foreach (var item in request.Items)
            {
                var part = await _dbContext.Parts
                    .FirstOrDefaultAsync(p => p.Id == item.PartId && !p.Isdeleted, cancellationToken);

                if (part == null)
                    return BadRequest(new { message = $"Product {item.PartId} not found" });

                // Catalog selling price — single source of truth, kept current by the price scheduler.
                decimal serverPrice;
                if (item.VariantId.HasValue)
                {
                    var variant = await _dbContext.Set<ProductVariant>()
                        .FirstOrDefaultAsync(v => v.Id == item.VariantId.Value && v.IsActive && !v.Isdeleted, cancellationToken);
                    if (variant == null)
                        return BadRequest(new { message = $"Variant not found for '{part.Name}'." });

                    serverPrice = CatalogPrice.Resolve(part.SellingPrice, variant.SellingPrice);
                }
                else
                {
                    serverPrice = CatalogPrice.Resolve(part.SellingPrice, null);
                }

                if (serverPrice <= 0)
                    return BadRequest(new { message = $"No price set for '{part.Name}'. Please set a selling price in the admin panel." });

                // Apply active discounts to get the effective (customer-facing) price
                var today = DateTime.UtcNow.Date;
                var activeDiscounts = await _dbContext.Discounts
                    .Where(d => d.IsActive && !d.Isdeleted
                        && d.StartDate.Date <= today
                        && (!d.EndDate.HasValue || d.EndDate.Value.Date >= today)
                        && (d.PartId == item.PartId || d.PartId == null)
                        && (d.ProductVariantId == null || d.ProductVariantId == item.VariantId))
                    .ToListAsync(cancellationToken);

                var effectivePrice = serverPrice;
                if (activeDiscounts.Any())
                {
                    var best = activeDiscounts
                        .Select(d => new { d, amt = d.CalculateDiscountAmount(serverPrice) })
                        .Where(x => x.amt > 0)
                        .OrderByDescending(x => x.amt)
                        .FirstOrDefault();
                    if (best != null) effectivePrice = serverPrice - best.amt;
                }

                // Validate cart price matches effective server price (prevent client-side manipulation)
                if (item.UnitPrice > 0 && Math.Abs(item.UnitPrice - effectivePrice) > 0.01m)
                    return BadRequest(new { message = $"The price for '{part.Name}' has changed. Please refresh your cart and try again." });

                lineRequests.Add((item.PartId, item.VariantId, item.Quantity, effectivePrice));
            }

            // 4. Resolve promo code discount (cart-level, before building SalesOrder)
            decimal promoDiscountAmount = 0;
            string appliedPromoCode = string.Empty;

            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                var promoToday = DateTime.UtcNow.Date;
                var promoEntry = await _dbContext.Discounts
                    .FirstOrDefaultAsync(d => d.IsActive && !d.Isdeleted
                        && d.PromoCode != null
                        && d.PromoCode.ToUpper() == request.PromoCode.Trim().ToUpper()
                        && d.PartId == null
                        && d.StartDate.Date <= promoToday
                        && (!d.EndDate.HasValue || d.EndDate.Value.Date >= promoToday),
                        cancellationToken);

                if (promoEntry == null)
                    return BadRequest(new { message = $"Promo code '{request.PromoCode}' is invalid or has expired." });

                var subtotalForPromo = lineRequests.Sum(l => l.qty * l.price);

                if (promoEntry.MinimumCartAmount.HasValue && subtotalForPromo < promoEntry.MinimumCartAmount.Value)
                    return BadRequest(new
                    {
                        message = $"This promo code requires a minimum order of {promoEntry.MinimumCartAmount.Value:N0} {request.Currency ?? "BDT"}."
                    });

                promoDiscountAmount = promoEntry.CalculateDiscountAmount(subtotalForPromo);
                appliedPromoCode = promoEntry.PromoCode!;
            }

            // 5. Build and persist the SalesOrder
            var soNumber = await _codeGenerateService.GenerateAsync("SO", cancellationToken);
            var deliveryAddress = $"{request.ShippingAddress}, {request.ShippingCity}".Trim(',', ' ');

            var salesOrder = SalesOrder.Create(
                soNumber,
                customer.Id,
                customer.FirstName + " " + customer.LastName,
                customer.Email,
                customer.Phone,
                warehouseId: warehouse?.Id,
                deliveryAddress: deliveryAddress,
                notes: request.Notes ?? string.Empty,
                currency: request.Currency ?? "BDT",
                channel: "ECOMMERCE"
            );

            int lineNumber = 1;
            foreach (var (partId, variantId, qty, price) in lineRequests)
            {
                var line = SalesOrderLine.Create(
                    salesOrder.Id,
                    partId,
                    qty,
                    price,
                    lineNumber++,
                    productVariantId: variantId
                );
                salesOrder.LineItems.Add(line);
            }

            salesOrder.CalculateTotal();

            if (promoDiscountAmount > 0)
                salesOrder.ApplyAdditionalDiscount(promoDiscountAmount);

            salesOrder.Confirm();
            salesOrder.CreatedBy = "ECOMMERCE";
            salesOrder.ModifiedBy = "ECOMMERCE";

            var paymentMode = request.PaymentMode?.Trim().ToUpper() ?? "CASH";
            var isCodOrCash = paymentMode is "CASH" or "COD" or "";
            decimal effectivePaid = isCodOrCash ? 0 : request.AmountPaid;

            if (isCodOrCash && request.AmountPaid > 0)
                return BadRequest(new { message = "Cash on delivery orders do not require upfront payment. Set AmountPaid to 0." });

            // Enforce full payment for non-COD online orders before the transaction
            if (!isCodOrCash && request.AmountPaid < salesOrder.GrandTotal)
                return BadRequest(new
                {
                    message = $"Online orders with {request.PaymentMode} payment require full payment. " +
                              $"Total is {salesOrder.Currency} {salesOrder.GrandTotal:N0}."
                });

            // Generate codes OUTSIDE the transaction — CodeGenerateService opens its own inner
            // transaction; nesting it inside the outer tx causes "connection already in a transaction".
            var invoiceNumber = await _codeGenerateService.GenerateAsync("INV", cancellationToken);
            var transactionNumber = effectivePaid > 0
                ? await _codeGenerateService.GenerateAsync("TXN", cancellationToken)
                : string.Empty;

            decimal paidAmount = 0;
            Invoice? savedInvoice = null;

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (isNewCustomer)
                        await _customerRepository.AddAsync(customer, cancellationToken);

                    await _salesOrderRepository.AddAsync(salesOrder, cancellationToken);

                    foreach (var (pId, vId, qty, _) in lineRequests)
                        await ConsumeStockForLineAsync(pId, vId, qty, soNumber, warehouse!.Id, cancellationToken);

                    // Return every session reservation's stock to available (not just mark the record
                    // released) — otherwise reserved units for cart items that weren't purchased, or
                    // quantities reduced before checkout, would be stranded and silently shrink availability.
                    if (!string.IsNullOrWhiteSpace(request.SessionId))
                        await ReleaseSessionReservationsAsync(request.SessionId, cancellationToken);

                    // Create and issue invoice using the pre-generated number
                    var invoice = Invoice.Create(invoiceNumber, salesOrder.Id, salesOrder.SubTotal,
                        salesOrder.TaxAmount, DateTime.UtcNow.AddDays(30),
                        request.Notes ?? string.Empty, salesOrder.Currency);
                    invoice.Issue();
                    invoice.CreatedBy = "ECOMMERCE";
                    invoice.ModifiedBy = "ECOMMERCE";
                    await _invoiceRepository.AddAsync(invoice, cancellationToken);
                    savedInvoice = invoice;

                    // Record payment for non-COD methods
                    if (effectivePaid > 0)
                    {
                        var payment = CustomerPayment.Create(
                            customer.Id, null, effectivePaid, paymentMode,
                            transactionNumber, request.PaymentReference ?? string.Empty, DateTime.UtcNow);
                        payment.LinkToInvoice(invoice.Id);
                        payment.MarkAsCompleted();
                        payment.MarkAsSettled("ECOMMERCE");
                        payment.CreatedBy = "ECOMMERCE";
                        payment.ModifiedBy = "ECOMMERCE";
                        await _customerPaymentRepository.AddAsync(payment, cancellationToken);

                        salesOrder.RecordPayment(effectivePaid);
                        if (salesOrder.PaymentStatus == "PAID")
                            salesOrder.MarkAsPaid();
                        salesOrder.ModifiedBy = "ECOMMERCE";
                        await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
                        paidAmount = effectivePaid;
                    }

                    // Update customer outstanding balance
                    customer.UpdateBalance(salesOrder.GrandTotal - effectivePaid);
                    customer.ModifiedBy = "ECOMMERCE";
                    await _customerRepository.UpdateAsync(customer, cancellationToken);

                    // Sync invoice payment status
                    var freshInvoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .FirstOrDefaultAsync(i => i.Id == invoice.Id, cancellationToken);
                    if (freshInvoice != null)
                    {
                        freshInvoice.UpdatePaymentStatus();
                        freshInvoice.ModifiedBy = "ECOMMERCE";
                        await _invoiceRepository.UpdateAsync(freshInvoice, cancellationToken);
                    }

                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            var dueBalance = salesOrder.GrandTotal - paidAmount;
            return Ok(new EcommerceCheckoutResponse
            {
                SalesOrderId = salesOrder.Id,
                SONumber = salesOrder.SONumber,
                CustomerName = customer.FirstName + " " + customer.LastName,
                GrandTotal = salesOrder.GrandTotal,
                AmountPaid = paidAmount,
                DueBalance = dueBalance,
                Currency = salesOrder.Currency,
                Status = salesOrder.Status,
                PaymentStatus = salesOrder.PaymentStatus,
                InvoiceNumber = invoiceNumber,
                Channel = salesOrder.Channel,
                PromoDiscountAmount = promoDiscountAmount,
                AppliedPromoCode = appliedPromoCode
            });
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
            _logger.LogError(ex, "Error processing e-commerce checkout");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    // ── Order Lookup (for confirmation page refresh) ─────────────────────────

    [HttpGet("orders/{soNumber}")]
    public async Task<IActionResult> GetOrderBySoNumber(string soNumber, CancellationToken cancellationToken)
    {
        var order = await _dbContext.SalesOrders
            .FirstOrDefaultAsync(so => so.SONumber == soNumber.ToUpper() && !so.Isdeleted, cancellationToken);

        if (order is null) return NotFound(new { message = "Order not found" });

        var invoice = await _dbContext.Invoices
            .Include(i => i.CustomerPayments)
            .Where(i => i.SalesOrderId == order.Id && !i.Isdeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new EcommerceCheckoutResponse
        {
            SalesOrderId = order.Id,
            SONumber = order.SONumber,
            CustomerName = order.CustomerName,
            GrandTotal = order.GrandTotal,
            AmountPaid = order.PaidAmount,
            DueBalance = order.GrandTotal - order.PaidAmount,
            Currency = order.Currency,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            InvoiceNumber = invoice?.InvoiceNumber ?? string.Empty,
            Channel = order.Channel
        });
    }

    // ── COD Cash Collection (at delivery) ────────────────────────────────────

    [HttpPost("orders/{soNumber}/collect-cod")]
    [Authorize(Roles = "Admin,Manager,User")]
    public async Task<IActionResult> CollectCod(string soNumber, [FromBody] CodCollectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.AmountCollected <= 0)
                return BadRequest(new { message = "AmountCollected must be greater than 0." });

            var order = await _dbContext.SalesOrders
                .FirstOrDefaultAsync(so => so.SONumber == soNumber.ToUpper() && !so.Isdeleted, cancellationToken);

            if (order is null) return NotFound(new { message = "Order not found." });
            if (order.Channel != "ECOMMERCE") return BadRequest(new { message = "COD collection is only applicable to online (ECOMMERCE) orders." });
            if (order.PaymentStatus == "PAID") return BadRequest(new { message = "This order has already been fully paid." });
            if (order.Status == "CANCELLED") return BadRequest(new { message = "Cannot collect payment for a cancelled order." });

            var outstanding = order.GrandTotal - order.PaidAmount;
            if (Math.Abs(request.AmountCollected - outstanding) > 0.01m)
                return BadRequest(new { message = $"Amount collected ({request.AmountCollected:N2}) must equal the outstanding balance ({outstanding:N2})." });

            var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found." });

            var invoice = await _dbContext.Invoices
                .Include(i => i.CustomerPayments)
                .FirstOrDefaultAsync(i => i.SalesOrderId == order.Id && !i.Isdeleted, cancellationToken);
            if (invoice is null) return BadRequest(new { message = "Invoice not found for this order." });

            var collectedBy = _currentUserService.GetCurrentUsername();
            var transactionNumber = await _codeGenerateService.GenerateAsync("TXN", cancellationToken);

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var payment = CustomerPayment.Create(
                        order.CustomerId, null, request.AmountCollected, "CASH",
                        transactionNumber, request.PaymentReference ?? string.Empty, DateTime.UtcNow);
                    payment.LinkToInvoice(invoice.Id);
                    payment.MarkAsCompleted();
                    payment.MarkAsSettled(collectedBy);
                    payment.CreatedBy = collectedBy;
                    payment.ModifiedBy = collectedBy;
                    await _customerPaymentRepository.AddAsync(payment, cancellationToken);

                    order.RecordPayment(request.AmountCollected);
                    if (order.PaymentStatus == "PAID")
                        order.MarkAsPaid();
                    order.ModifiedBy = collectedBy;
                    await _salesOrderRepository.UpdateAsync(order, cancellationToken);

                    customer.UpdateBalance(-request.AmountCollected);
                    customer.ModifiedBy = collectedBy;
                    await _customerRepository.UpdateAsync(customer, cancellationToken);

                    var freshInvoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .FirstOrDefaultAsync(i => i.Id == invoice.Id, cancellationToken);
                    if (freshInvoice != null)
                    {
                        freshInvoice.UpdatePaymentStatus();
                        freshInvoice.ModifiedBy = collectedBy;
                        await _invoiceRepository.UpdateAsync(freshInvoice, cancellationToken);
                    }

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
                message = "COD payment collected successfully.",
                soNumber = order.SONumber,
                amountCollected = request.AmountCollected,
                orderStatus = order.Status,
                paymentStatus = order.PaymentStatus
            });
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
            _logger.LogError(ex, "Error collecting COD payment for order {SONumber}", soNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    // ── In-Store (Salesperson-Assisted) Checkout ──────────────────────────────

    [HttpPost("instore-checkout")]
    [Authorize(Roles = "Admin,Manager,User")]
    public async Task<IActionResult> InstoreCheckout([FromBody] InstoreCheckoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return BadRequest(new { message = "Customer name is required" });

            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
                return BadRequest(new { message = "Customer phone is required" });

            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "Cart is empty" });

            if (request.AmountPaid < 0)
                return BadRequest(new { message = "Amount paid cannot be negative" });

            var discountType = request.DiscountType?.Trim().ToUpper() ?? "NONE";
            if (discountType is not ("NONE" or "PERCENTAGE" or "FIXED"))
                return BadRequest(new { message = "DiscountType must be NONE, PERCENTAGE, or FIXED" });

            if (discountType == "PERCENTAGE" && request.DiscountValue is < 0 or > 100)
                return BadRequest(new { message = "Discount percentage must be between 0 and 100" });

            if (discountType == "FIXED" && request.DiscountValue < 0)
                return BadRequest(new { message = "Fixed discount amount cannot be negative" });

            if (discountType != "NONE" && request.DiscountValue > 0 && string.IsNullOrWhiteSpace(request.DiscountReason))
                return BadRequest(new { message = "Discount reason is required when applying a discount" });

            var salespersonName = _currentUserService.GetCurrentUsername();

            var (customer, isNewCustomer) = await ResolveCustomerAsync(request, cancellationToken);

            var warehouse = await _dbContext.Warehouses
                .Where(w => w.IsActive && !w.Isdeleted)
                .OrderBy(w => w.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (warehouse == null)
                return BadRequest(new { message = "No active warehouse is configured. Please contact the administrator." });

            var lineRequests = new List<(Guid partId, Guid? variantId, int qty, decimal price)>();
            foreach (var item in request.Items)
            {
                var part = await _dbContext.Parts
                    .FirstOrDefaultAsync(p => p.Id == item.PartId && !p.Isdeleted, cancellationToken);

                if (part == null)
                    return BadRequest(new { message = $"Product {item.PartId} not found" });

                decimal serverPrice;
                if (item.VariantId.HasValue)
                {
                    var variant = await _dbContext.Set<ProductVariant>()
                        .FirstOrDefaultAsync(v => v.Id == item.VariantId.Value && v.IsActive && !v.Isdeleted, cancellationToken);
                    if (variant == null)
                        return BadRequest(new { message = $"Variant not found for '{part.Name}'." });

                    serverPrice = CatalogPrice.Resolve(part.SellingPrice, variant.SellingPrice);
                }
                else
                {
                    serverPrice = CatalogPrice.Resolve(part.SellingPrice, null);
                }

                if (serverPrice <= 0)
                    return BadRequest(new { message = $"No price set for '{part.Name}'." });

                var today = DateTime.UtcNow.Date;
                var activeDiscounts = await _dbContext.Discounts
                    .Where(d => d.IsActive && !d.Isdeleted
                        && d.StartDate.Date <= today
                        && (!d.EndDate.HasValue || d.EndDate.Value.Date >= today)
                        && (d.PartId == item.PartId || d.PartId == null)
                        && (d.ProductVariantId == null || d.ProductVariantId == item.VariantId))
                    .ToListAsync(cancellationToken);

                var effectivePrice = serverPrice;
                if (activeDiscounts.Any())
                {
                    var best = activeDiscounts
                        .Select(d => new { d, amt = d.CalculateDiscountAmount(serverPrice) })
                        .Where(x => x.amt > 0)
                        .OrderByDescending(x => x.amt)
                        .FirstOrDefault();
                    if (best != null) effectivePrice = serverPrice - best.amt;
                }

                if (item.UnitPrice > 0 && Math.Abs(item.UnitPrice - effectivePrice) > 0.01m)
                    return BadRequest(new { message = $"The price for '{part.Name}' has changed. Please refresh and try again." });

                lineRequests.Add((item.PartId, item.VariantId, item.Quantity, effectivePrice));
            }

            // Resolve promo code discount (optional; stacks with salesperson discount)
            decimal promoDiscountAmountPos = 0;
            string appliedPromoCodePos = string.Empty;

            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                var promoToday = DateTime.UtcNow.Date;
                var promoEntry = await _dbContext.Discounts
                    .FirstOrDefaultAsync(d => d.IsActive && !d.Isdeleted
                        && d.PromoCode != null
                        && d.PromoCode.ToUpper() == request.PromoCode.Trim().ToUpper()
                        && d.PartId == null
                        && d.StartDate.Date <= promoToday
                        && (!d.EndDate.HasValue || d.EndDate.Value.Date >= promoToday),
                        cancellationToken);

                if (promoEntry == null)
                    return BadRequest(new { message = $"Promo code '{request.PromoCode}' is invalid or has expired." });

                var subtotalForPromo = lineRequests.Sum(l => l.qty * l.price);

                if (promoEntry.MinimumCartAmount.HasValue && subtotalForPromo < promoEntry.MinimumCartAmount.Value)
                    return BadRequest(new
                    {
                        message = $"This promo code requires a minimum order of {promoEntry.MinimumCartAmount.Value:N0} {request.Currency ?? "BDT"}."
                    });

                promoDiscountAmountPos = promoEntry.CalculateDiscountAmount(subtotalForPromo);
                appliedPromoCodePos = promoEntry.PromoCode!;
            }

            var soNumber = await _codeGenerateService.GenerateAsync("SO", cancellationToken);
            var deliveryAddress = $"{request.ShippingAddress}, {request.ShippingCity}".Trim(',', ' ');

            // Build discount annotation for notes
            var discountNote = string.Empty;
            if (discountType != "NONE" && request.DiscountValue > 0)
            {
                var discountDesc = discountType == "PERCENTAGE"
                    ? $"{request.DiscountValue}%"
                    : $"{request.Currency ?? "BDT"} {request.DiscountValue:N2}";
                discountNote = $"[Discount: {discountDesc} applied by {salespersonName}" +
                               (string.IsNullOrWhiteSpace(request.DiscountReason) ? "]" : $" | Reason: {request.DiscountReason}]");
            }
            if (!string.IsNullOrWhiteSpace(appliedPromoCodePos))
                discountNote += $" [Promo: {appliedPromoCodePos} = {request.Currency ?? "BDT"} {promoDiscountAmountPos:N2}]";

            var combinedNotes = string.IsNullOrWhiteSpace(discountNote)
                ? request.Notes ?? string.Empty
                : string.IsNullOrWhiteSpace(request.Notes) ? discountNote : $"{request.Notes}\n{discountNote}";

            var salesOrder = SalesOrder.Create(
                soNumber, customer.Id,
                customer.FirstName + " " + customer.LastName,
                customer.Email, customer.Phone,
                warehouseId: warehouse?.Id,
                deliveryAddress: deliveryAddress,
                notes: combinedNotes,
                currency: request.Currency ?? "BDT",
                channel: "POS"
            );

            // Optionally link the customer's vehicle this purchase is for
            if (request.CustomerVehicleId.HasValue && request.CustomerVehicleId.Value != Guid.Empty)
            {
                var vehicle = await _customerVehicleRepository.GetByIdAsync(request.CustomerVehicleId.Value, cancellationToken);
                if (vehicle is null)
                    return BadRequest(new { message = "The selected vehicle was not found" });
                if (vehicle.CustomerId != customer.Id)
                    return BadRequest(new { message = "The selected vehicle does not belong to this customer" });
                salesOrder.SetVehicle(vehicle.Id, vehicle.GetLabel());
            }

            int lineNumber = 1;
            foreach (var (partId, variantId, qty, price) in lineRequests)
            {
                var line = SalesOrderLine.Create(salesOrder.Id, partId, qty, price, lineNumber++,
                    productVariantId: variantId);
                salesOrder.LineItems.Add(line);
            }

            // Apply salesperson discount before confirming
            if (discountType == "PERCENTAGE" && request.DiscountValue > 0)
                salesOrder.SetDiscountPercentage(request.DiscountValue);

            salesOrder.CalculateTotal();

            if (discountType == "FIXED" && request.DiscountValue > 0)
                salesOrder.ApplyAdditionalDiscount(request.DiscountValue);

            if (promoDiscountAmountPos > 0)
                salesOrder.ApplyAdditionalDiscount(promoDiscountAmountPos);

            if (request.AmountPaid > salesOrder.GrandTotal)
                return BadRequest(new { message = $"Amount paid ({request.AmountPaid:N2}) cannot exceed order total ({salesOrder.GrandTotal:N2})" });

            salesOrder.Confirm();
            salesOrder.CreatedBy = salespersonName;
            salesOrder.ModifiedBy = salespersonName;

            var paymentMode = request.PaymentMode?.Trim().ToUpper() ?? "CASH";

            // Generate codes OUTSIDE the transaction (same reason as online checkout)
            var invoiceNumber = await _codeGenerateService.GenerateAsync("INV", cancellationToken);
            var transactionNumber = request.AmountPaid > 0
                ? await _codeGenerateService.GenerateAsync("TXN", cancellationToken)
                : string.Empty;

            decimal paidAmount = 0;

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (isNewCustomer)
                        await _customerRepository.AddAsync(customer, cancellationToken);

                    await _salesOrderRepository.AddAsync(salesOrder, cancellationToken);

                    foreach (var (pId, vId, qty, _) in lineRequests)
                        await ConsumeStockForLineAsync(pId, vId, qty, soNumber, warehouse!.Id, cancellationToken);

                    // Return every session reservation's stock to available (not just mark the record
                    // released) — otherwise reserved units for cart items that weren't purchased, or
                    // quantities reduced before checkout, would be stranded and silently shrink availability.
                    if (!string.IsNullOrWhiteSpace(request.SessionId))
                        await ReleaseSessionReservationsAsync(request.SessionId, cancellationToken);

                    var invoice = Invoice.Create(invoiceNumber, salesOrder.Id, salesOrder.SubTotal,
                        salesOrder.TaxAmount, DateTime.UtcNow.AddDays(30),
                        combinedNotes, salesOrder.Currency);
                    invoice.Issue();

                    // Propagate salesperson discount to invoice so invoice GrandTotal matches SO
                    if (salesOrder.DiscountAmount > 0)
                        invoice.SetDiscount(salesOrder.DiscountAmount);

                    invoice.CreatedBy = salespersonName;
                    invoice.ModifiedBy = salespersonName;
                    await _invoiceRepository.AddAsync(invoice, cancellationToken);

                    if (request.AmountPaid > 0)
                    {
                        var payment = CustomerPayment.Create(
                            customer.Id, null, request.AmountPaid, paymentMode,
                            transactionNumber, request.PaymentReference ?? string.Empty, DateTime.UtcNow);
                        payment.LinkToInvoice(invoice.Id);
                        payment.MarkAsCompleted();
                        payment.MarkAsSettled(salespersonName);
                        payment.CreatedBy = salespersonName;
                        payment.ModifiedBy = salespersonName;
                        await _customerPaymentRepository.AddAsync(payment, cancellationToken);

                        salesOrder.RecordPayment(request.AmountPaid);
                        if (salesOrder.PaymentStatus == "PAID")
                            salesOrder.MarkAsPaid();
                        salesOrder.ModifiedBy = salespersonName;
                        await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
                        paidAmount = request.AmountPaid;
                    }

                    customer.UpdateBalance(salesOrder.GrandTotal - paidAmount);
                    customer.ModifiedBy = salespersonName;
                    await _customerRepository.UpdateAsync(customer, cancellationToken);

                    var freshInvoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .FirstOrDefaultAsync(i => i.Id == invoice.Id, cancellationToken);
                    if (freshInvoice != null)
                    {
                        freshInvoice.UpdatePaymentStatus();
                        if (paidAmount == 0)
                            freshInvoice.MarkAsDue();
                        freshInvoice.ModifiedBy = salespersonName;
                        await _invoiceRepository.UpdateAsync(freshInvoice, cancellationToken);
                    }

                    // Audit trail: salesperson discount
                    if (salesOrder.DiscountAmount > 0)
                    {
                        await _dbContext.AuditLogs.AddAsync(new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            EntityName = "SalesOrder",
                            EntityId = salesOrder.Id.ToString(),
                            Action = "DISCOUNT_APPLIED",
                            PropertyName = "DiscountAmount",
                            OldValue = "0",
                            NewValue = salesOrder.DiscountAmount.ToString("F2"),
                            PerformedBy = salespersonName,
                            PerformedAt = DateTime.UtcNow,
                            UserAgent = $"Type:{discountType}|Value:{request.DiscountValue}|Reason:{request.DiscountReason ?? string.Empty}"
                        }, cancellationToken);
                    }

                    // Audit trail: due/credit balance
                    var dueAtCommit = salesOrder.GrandTotal - paidAmount;
                    if (dueAtCommit > 0)
                    {
                        await _dbContext.AuditLogs.AddAsync(new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            EntityName = "SalesOrder",
                            EntityId = salesOrder.Id.ToString(),
                            Action = "DUE_CREDIT_RECORDED",
                            PropertyName = "DueBalance",
                            OldValue = "0",
                            NewValue = dueAtCommit.ToString("F2"),
                            PerformedBy = salespersonName,
                            PerformedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            var dueBalance = salesOrder.GrandTotal - paidAmount;
            return Ok(new EcommerceCheckoutResponse
            {
                SalesOrderId = salesOrder.Id,
                SONumber = salesOrder.SONumber,
                CustomerName = customer.FirstName + " " + customer.LastName,
                GrandTotal = salesOrder.GrandTotal,
                AmountPaid = paidAmount,
                DueBalance = dueBalance,
                Currency = salesOrder.Currency,
                Status = salesOrder.Status,
                PaymentStatus = salesOrder.PaymentStatus,
                InvoiceNumber = invoiceNumber,
                Channel = salesOrder.Channel,
                DiscountAmount = salesOrder.DiscountAmount,
                DiscountType = discountType == "NONE" ? string.Empty : discountType,
                DiscountReason = request.DiscountReason ?? string.Empty,
                PromoDiscountAmount = promoDiscountAmountPos,
                AppliedPromoCode = appliedPromoCodePos
            });
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
            _logger.LogError(ex, "Error processing in-store checkout");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    // ── Stock Reservation ─────────────────────────────────────────────────────

    [HttpPost("stock/reserve")]
    public async Task<IActionResult> Reserve([FromBody] CartReserveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
                return BadRequest(new { message = "SessionId is required" });

            if (request.PartId == Guid.Empty || request.Quantity <= 0)
                return BadRequest(new { message = "Valid PartId and Quantity are required" });

            // Optimistic-concurrency retry: concurrent reservers bump StockLevel.RowVersion, so a
            // racing write fails with DbUpdateConcurrencyException — reload and retry to avoid oversell.
            const int maxReserveAttempts = 3;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await CleanupExpiredReservationsAsync(request.PartId, cancellationToken);

                    // If no variantId was supplied (e.g. item added from landing page),
                    // auto-pick the variant with the most available stock.
                    if (!request.VariantId.HasValue)
                    {
                        var bestVariant = await (
                            from v in _dbContext.ProductVariants
                            where v.PartId == request.PartId && !v.Isdeleted && v.IsActive
                            join sl in _dbContext.StockLevels
                                on v.Id equals sl.VariantId
                            where !sl.Isdeleted && sl.IsActive
                            group sl by v.Id into g
                            where g.Sum(x => x.QuantityOnHand - x.QuantityReserved) >= request.Quantity
                            orderby g.Sum(x => x.QuantityOnHand - x.QuantityReserved) descending
                            select g.Key
                        ).FirstOrDefaultAsync(cancellationToken);

                        if (bestVariant != default)
                            request.VariantId = bestVariant;
                    }

                    // Unified stock: rows live in StockLevels scoped by (PartId, VariantId?). VariantId null = part-level.
                    var stockLevels = await _dbContext.StockLevels
                        .Where(sl => sl.PartId == request.PartId && sl.VariantId == request.VariantId && sl.IsActive && !sl.Isdeleted)
                        .ToListAsync(cancellationToken);

                    var totalAvailable = stockLevels.Sum(sl => sl.QuantityAvailable);

                    if (totalAvailable < request.Quantity)
                        return BadRequest(new { message = $"Only {totalAvailable} unit(s) available", available = totalAvailable });

                    var existing = await _dbContext.CartReservations
                        .Where(r => r.SessionId == request.SessionId
                                 && r.PartId == request.PartId
                                 && r.ProductVariantId == request.VariantId
                                 && !r.IsReleased)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existing != null)
                    {
                        var delta = request.Quantity - existing.Quantity;

                        if (delta > 0)
                        {
                            // Spread across levels — availability was summed across all of them, so a
                            // single level may not hold the whole delta even when the total does.
                            var toReserve = delta;
                            foreach (var sl in stockLevels.OrderByDescending(s => s.QuantityAvailable))
                            {
                                if (toReserve <= 0) break;
                                var canReserve = Math.Min(sl.QuantityAvailable, toReserve);
                                if (canReserve > 0) { sl.ReserveStock(canReserve); toReserve -= canReserve; }
                            }
                        }
                        else if (delta < 0)
                        {
                            var toRelease = -delta;
                            foreach (var sl in stockLevels)
                            {
                                if (toRelease <= 0) break;
                                var r = Math.Min(sl.QuantityReserved, toRelease);
                                if (r > 0) { sl.ReleaseReservedStock(r); toRelease -= r; }
                            }
                        }

                        existing.UpdateQuantity(request.Quantity);
                        existing.ExtendTtl();
                    }
                    else
                    {
                        var reservation = CartReservation.Create(
                            request.SessionId, request.PartId, request.Quantity,
                            productVariantId: request.VariantId);

                        // Spread across levels — availability was summed across all of them, so a single
                        // level may not hold the whole quantity even when the total does.
                        var toReserve = request.Quantity;
                        foreach (var sl in stockLevels.OrderByDescending(s => s.QuantityAvailable))
                        {
                            if (toReserve <= 0) break;
                            var canReserve = Math.Min(sl.QuantityAvailable, toReserve);
                            if (canReserve > 0) { sl.ReserveStock(canReserve); toReserve -= canReserve; }
                        }

                        await _dbContext.CartReservations.AddAsync(reservation, cancellationToken);
                    }

                    // Single SaveChangesAsync = implicit atomic transaction
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return Ok(new { reserved = true, quantity = request.Quantity });
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxReserveAttempts)
                {
                    _dbContext.ChangeTracker.Clear(); // discard stale tracked state, then retry
                }
            } // end optimistic-concurrency retry loop
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            // Lost the race after all retries — let the caller reload and try again.
            return Conflict(ApiError.Conflict("Stock was just updated by another request. Please try again.", HttpContext.Request.Path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for part {PartId}", request.PartId);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("stock/release")]
    public async Task<IActionResult> Release([FromBody] CartReleaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var reservation = await _dbContext.CartReservations
                .Where(r => r.SessionId == request.SessionId
                         && r.PartId == request.PartId
                         && r.ProductVariantId == request.VariantId
                         && !r.IsReleased)
                .FirstOrDefaultAsync(cancellationToken);

            if (reservation == null)
                return Ok(new { released = true }); // already gone — idempotent

            var qtyToRelease = reservation.Quantity;

            // Unified: release against StockLevels scoped to the reservation's (PartId, VariantId?).
            var stockLevels = await _dbContext.StockLevels
                .Where(sl => sl.PartId == request.PartId && sl.VariantId == reservation.ProductVariantId && sl.IsActive && !sl.Isdeleted)
                .ToListAsync(cancellationToken);

            foreach (var sl in stockLevels)
            {
                if (qtyToRelease <= 0) break;
                var canRelease = Math.Min(sl.QuantityReserved, qtyToRelease);
                if (canRelease > 0) { sl.ReleaseReservedStock(canRelease); qtyToRelease -= canRelease; }
            }

            reservation.Release();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { released = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing stock for part {PartId}", request.PartId);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("stock/release-session")]
    public async Task<IActionResult> ReleaseSession([FromBody] CartReleaseSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
                return BadRequest(new { message = "SessionId is required" });

            var reservations = await _dbContext.CartReservations
                .Where(r => r.SessionId == request.SessionId && !r.IsReleased)
                .ToListAsync(cancellationToken);

            if (!reservations.Any())
                return Ok(new { released = 0 });

            foreach (var reservation in reservations)
            {
                var qtyToRelease = reservation.Quantity;

                // Unified: release against StockLevels scoped to the reservation's (PartId, VariantId?).
                var stockLevels = await _dbContext.StockLevels
                    .Where(sl => sl.PartId == reservation.PartId && sl.VariantId == reservation.ProductVariantId && sl.IsActive && !sl.Isdeleted)
                    .ToListAsync(cancellationToken);

                foreach (var sl in stockLevels)
                {
                    if (qtyToRelease <= 0) break;
                    var canRelease = Math.Min(sl.QuantityReserved, qtyToRelease);
                    if (canRelease > 0) { sl.ReleaseReservedStock(canRelease); qtyToRelease -= canRelease; }
                }

                reservation.Release();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { released = reservations.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing session reservations for {SessionId}", request.SessionId);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    private async Task CleanupExpiredReservationsAsync(Guid partId, CancellationToken ct)
    {
        var expired = await _dbContext.CartReservations
            .Where(r => r.PartId == partId && !r.IsReleased && r.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct);

        if (!expired.Any()) return;

        // All StockLevels for this part (any variant); each reservation releases against its own variant scope.
        var stockLevels = await _dbContext.StockLevels
            .Where(sl => sl.PartId == partId && sl.IsActive && !sl.Isdeleted)
            .ToListAsync(ct);

        foreach (var reservation in expired)
        {
            var qtyToRelease = reservation.Quantity;

            foreach (var sl in stockLevels.Where(s => s.VariantId == reservation.ProductVariantId))
            {
                if (qtyToRelease <= 0) break;
                var canRelease = Math.Min(sl.QuantityReserved, qtyToRelease);
                if (canRelease > 0) { sl.ReleaseReservedStock(canRelease); qtyToRelease -= canRelease; }
            }

            reservation.Release();
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Releases every active cart reservation for a session back to stock, then marks the records released.
    /// Whatever the order actually consumed was already drawn down in ConsumeStockForLineAsync, so the
    /// Math.Min(reserved, qty) below only returns the *leftover* reserved units — this prevents reserved
    /// stock from being stranded when the order lines don't exactly match the session's reservations
    /// (item removed from cart, quantity reduced, etc.).
    /// </summary>
    private async Task ReleaseSessionReservationsAsync(string sessionId, CancellationToken ct)
    {
        var reservations = await _dbContext.CartReservations
            .Where(r => r.SessionId == sessionId && !r.IsReleased)
            .ToListAsync(ct);

        if (!reservations.Any()) return;

        foreach (var reservation in reservations)
        {
            var qtyToRelease = reservation.Quantity;

            var stockLevels = await _dbContext.StockLevels
                .Where(sl => sl.PartId == reservation.PartId && sl.VariantId == reservation.ProductVariantId && sl.IsActive && !sl.Isdeleted)
                .ToListAsync(ct);

            foreach (var sl in stockLevels)
            {
                if (qtyToRelease <= 0) break;
                var canRelease = Math.Min(sl.QuantityReserved, qtyToRelease);
                if (canRelease > 0) { sl.ReleaseReservedStock(canRelease); qtyToRelease -= canRelease; }
            }

            reservation.Release();
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task ConsumeStockForLineAsync(
        Guid partId, Guid? variantId, int qty,
        string soNumber, Guid warehouseId, CancellationToken ct)
    {
        var reference = $"Ecommerce Order {soNumber}";

        // Unified: stock lives in StockLevels scoped by (PartId, VariantId?). VariantId null = part-level.
        var partLevels = await _dbContext.StockLevels
            .Where(s => s.PartId == partId && s.VariantId == variantId && s.IsActive && !s.Isdeleted)
            .ToListAsync(ct);

        if (!partLevels.Any())
            throw new InvalidOperationException(
                $"No stock levels configured for part {partId}{(variantId.HasValue ? $" / variant {variantId}" : "")}. Cannot fulfil this order line.");

        var releaseRemaining = qty;
        foreach (var s in partLevels.OrderByDescending(x => x.QuantityReserved))
        {
            if (releaseRemaining <= 0) break;
            var r = Math.Min(s.QuantityReserved, releaseRemaining);
            if (r > 0) { s.ReleaseReservedStock(r); releaseRemaining -= r; }
        }
        var removeRemaining = qty;
        foreach (var s in partLevels.OrderByDescending(x => x.QuantityAvailable))
        {
            if (removeRemaining <= 0) break;
            var r = Math.Min(s.QuantityAvailable, removeRemaining);
            if (r > 0) { s.RemoveStock(r); removeRemaining -= r; }
        }
        if (removeRemaining > 0)
            throw new InvalidOperationException(
                $"Insufficient stock for part {partId}: ordered {qty}, only {qty - removeRemaining} available.");

        // StockMovement audit record
        var primaryLevel = partLevels.FirstOrDefault(s => s.WarehouseId == warehouseId) ?? partLevels.First();
        var stockMovement = StockMovement.Create(primaryLevel.Id, "OUT", qty, reference, soNumber, quantityInBaseUnit: qty);
        stockMovement.Approve("ECOMMERCE");
        stockMovement.CreatedBy = "ECOMMERCE";
        stockMovement.ModifiedBy = "ECOMMERCE";
        await _dbContext.StockMovements.AddAsync(stockMovement, ct);

        // FIFO lot deduction
        try
        {
            var lots = await _dbContext.StockLots
                .Where(l => l.PartId == partId && l.VariantId == variantId && l.WarehouseId == warehouseId
                    && l.QuantityAvailableInBaseUnit > 0 && !l.Isdeleted)
                .OrderBy(l => l.ReceivingDate)
                .ToListAsync(ct);

            var lotRemaining = qty;
            foreach (var lot in lots)
            {
                if (lotRemaining <= 0) break;
                var deduct = Math.Min(lot.QuantityAvailableInBaseUnit, lotRemaining);
                lot.RemoveStock(deduct, deduct, reference);
                lot.ModifiedBy = "ECOMMERCE";
                var lotMovement = StockLotMovement.Create(lot.Id, deduct, "SALE",
                    partId, "EcommerceOrder", DateTime.UtcNow, lot.CostPrice, reference);
                lotMovement.CreatedBy = "ECOMMERCE";
                lotMovement.ModifiedBy = "ECOMMERCE";
                await _dbContext.StockLotMovements.AddAsync(lotMovement, ct);
                lotRemaining -= deduct;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not deduct stock lots for ecommerce order {SONumber}, part {PartId}", soNumber, partId);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<(Customer customer, bool isNew)> ResolveCustomerAsync(EcommerceCheckoutRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            var existing = await _customerRepository.GetByEmailAsync(request.CustomerEmail, ct);
            if (existing != null) return (existing, false);
        }

        var nameParts = (request.CustomerName ?? "Guest").Trim().Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "Customer";

        var customerCode = await _codeGenerateService.GenerateAsync("CUST", ct);

        return (Customer.Create(
            customerCode,
            firstName,
            lastName,
            request.CustomerEmail ?? string.Empty,
            request.CustomerPhone,
            companyName: string.Empty,
            billingAddress: request.ShippingAddress ?? string.Empty,
            shippingAddress: request.ShippingAddress ?? string.Empty,
            city: request.ShippingCity ?? string.Empty,
            state: string.Empty,
            postalCode: request.ShippingPostalCode ?? string.Empty,
            country: request.ShippingCountry ?? "Bangladesh"
        ), true);
    }
}

public class EcommerceCheckoutRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
    public string? Notes { get; set; }
    public string? Currency { get; set; }
    public string? SessionId { get; set; }
    public string? PaymentMode { get; set; }       // CASH (COD), MOBILE_BANKING, CARD, etc.
    public decimal AmountPaid { get; set; } = 0;   // 0 = COD; must equal GrandTotal for non-COD online
    public string? PaymentReference { get; set; }  // bKash/Nagad transaction id, card last-4, etc.
    public string? PromoCode { get; set; }
    public List<EcommerceOrderItem> Items { get; set; } = new();
}

public class EcommerceOrderItem
{
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class EcommerceCheckoutResponse
{
    public Guid SalesOrderId { get; set; }
    public string SONumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal DueBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    // Salesperson discount (POS only; zero/empty for online orders)
    public decimal DiscountAmount { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public string DiscountReason { get; set; } = string.Empty;
    // Promo code discount (online + POS)
    public decimal PromoDiscountAmount { get; set; }
    public string AppliedPromoCode { get; set; } = string.Empty;
}

public class CartReserveRequest
{
    public string SessionId { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; }
}

public class CartReleaseRequest
{
    public string SessionId { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
}

public class CartReleaseSessionRequest
{
    public string SessionId { get; set; } = string.Empty;
}

// In-store (POS) checkout — extends online checkout with salesperson discount support.
// Partial AmountPaid is allowed (due/credit); AmountPaid = 0 means fully on credit.
public class InstoreCheckoutRequest : EcommerceCheckoutRequest
{
    /// <summary>NONE | PERCENTAGE | FIXED</summary>
    public string DiscountType { get; set; } = "NONE";

    /// <summary>Discount value: percentage (0-100) or fixed currency amount</summary>
    public decimal DiscountValue { get; set; } = 0;

    /// <summary>Reason for applying the discount (required for audit trail when discount is applied)</summary>
    public string? DiscountReason { get; set; }
    // PromoCode inherited from EcommerceCheckoutRequest

    /// <summary>Optional: the customer's vehicle this purchase is for (per-vehicle history).</summary>
    public Guid? CustomerVehicleId { get; set; }
}

public class CodCollectionRequest
{
    public decimal AmountCollected { get; set; }
    public string? PaymentReference { get; set; }
}
