using AutoPartShop.Application.DTOs.Notification;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private const string SmsKey = "NOTIFICATION:SMS_ENABLED";
    private const string WhatsAppKey = "NOTIFICATION:WHATSAPP_ENABLED";
    private const string SignalRRolesKey = "NOTIFICATION:SIGNALR_ROLES";
    private const string NotifCategory = "NOTIFICATION";

    private readonly INotificationService _notificationService;
    private readonly IApplicationSettingsRepository _settings;
    private readonly ISaleEventBroadcaster _broadcaster;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        IApplicationSettingsRepository settings,
        ISaleEventBroadcaster broadcaster,
        ICustomerRepository customerRepository,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _settings = settings;
        _broadcaster = broadcaster;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    /// <summary>Dev helper: push a fake sale notification to all connected staff via SignalR.</summary>
    [HttpPost("test-signalr")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> TestSignalR(CancellationToken cancellationToken)
    {
        await _broadcaster.BroadcastAsync(new SaleNotificationEvent
        {
            SalesOrderId = Guid.NewGuid(),
            SONumber = "SO-TEST-001",
            CustomerName = "SignalR Test",
            GrandTotal = 1234.56m,
            Currency = "BDT",
            SaleChannel = "POS",
            SaleType = "SALE",
            OccurredAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "test"
        }, cancellationToken);

        return Ok(new { message = "Test notification sent to staff group" });
    }

    /// <summary>
    /// Manually run the reorder-level scan and broadcast the low-stock alert to staff.
    /// Same scan the daily background job runs — useful for testing and ad-hoc checks.
    /// </summary>
    [HttpPost("reorder-alert/run")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RunReorderAlert(
        [FromServices] Services.ReorderAlertScanner scanner,
        CancellationToken cancellationToken)
    {
        var evt = await scanner.ScanAndBroadcastAsync(cancellationToken);
        return Ok(new
        {
            itemCount = evt?.ItemCount ?? 0,
            broadcast = evt != null,
            message = evt != null
                ? $"Reorder alert sent: {evt.ItemCount} item(s) at/below reorder level"
                : "No items at/below reorder level — nothing broadcast"
        });
    }

    /// <summary>Staff-triggered: send invoice HTML email to the customer for a sales order.</summary>
    [HttpPost("send-invoice-email/{salesOrderId:guid}")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> SendInvoiceEmail(Guid salesOrderId, CancellationToken cancellationToken)
    {
        var db = HttpContext.RequestServices.GetRequiredService<AutoPartDbContext>();

        var order = await db.SalesOrders
            .Include(o => o.LineItems).ThenInclude(l => l.Part)
            .Include(o => o.Invoice)
            .FirstOrDefaultAsync(o => o.Id == salesOrderId && !o.Isdeleted, cancellationToken);

        if (order is null)
            return NotFound(new { message = "Sales order not found" });

        if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            return BadRequest(new { message = "This order has no customer email address" });

        try
        {
            var subject = $"Invoice for Order {order.SONumber}";
            var htmlBody = BuildInvoiceHtml(order);

            // The controller owns the business decision to send this email;
            // the notification service just handles delivery and logging.
            await _notificationService.SendEmailAsync(order.CustomerEmail, subject, htmlBody, cancellationToken);

            return Ok(new { message = "Invoice email sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice email for order {Id}", salesOrderId);
            return StatusCode(500, new { message = "Failed to send invoice email" });
        }
    }

    /// <summary>Staff-triggered: remind a customer about their outstanding payment due.</summary>
    [HttpPost("send-payment-reminder/{customerId:guid}")]
    [HasPermission(Permissions.SalesProcessPayment)]
    public async Task<IActionResult> SendPaymentReminder(
        Guid customerId,
        [FromBody] SendPaymentReminderRequest? request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
            return NotFound(new { message = "Customer not found" });

        var channel = (request?.Channel ?? "SMS").Trim().ToUpperInvariant();
        var due = customer.CurrentBalance;
        var name = customer.GetFullName();

        // Compose a friendly default reminder when no custom message is supplied.
        var message = string.IsNullOrWhiteSpace(request?.Message)
            ? $"Dear {name}, your outstanding due at Sujan Motors is BDT {due:N2}. " +
              "Please clear it at your earliest convenience. Thank you."
            : request!.Message!;

        try
        {
            switch (channel)
            {
                case "SMS":
                    if (string.IsNullOrWhiteSpace(customer.Phone))
                        return BadRequest(new { message = "Customer has no phone number" });
                    if (!await IsEnabled(SmsKey, cancellationToken))
                        return BadRequest(new { message = "SMS channel is disabled in notification settings" });
                    await _notificationService.SendSmsAsync(customer.Phone, message, cancellationToken);
                    return Ok(new { message = "Payment reminder sent by SMS", channel, recipient = customer.Phone, due });

                case "WHATSAPP":
                    if (string.IsNullOrWhiteSpace(customer.Phone))
                        return BadRequest(new { message = "Customer has no phone number" });
                    if (!await IsEnabled(WhatsAppKey, cancellationToken))
                        return BadRequest(new { message = "WhatsApp channel is disabled in notification settings" });
                    await _notificationService.SendWhatsAppAsync(customer.Phone, message, cancellationToken);
                    return Ok(new { message = "Payment reminder sent by WhatsApp", channel, recipient = customer.Phone, due });

                case "EMAIL":
                    if (string.IsNullOrWhiteSpace(customer.Email))
                        return BadRequest(new { message = "Customer has no email address" });
                    var html = $"<p>Dear <strong>{System.Net.WebUtility.HtmlEncode(name)}</strong>,</p>" +
                               $"<p>Your outstanding due at <strong>Sujan Motors</strong> is " +
                               $"<strong>BDT {due:N2}</strong>.</p>" +
                               "<p>Please clear it at your earliest convenience. Thank you.</p>";
                    await _notificationService.SendEmailAsync(customer.Email, "Payment Reminder — Sujan Motors", html, cancellationToken);
                    return Ok(new { message = "Payment reminder sent by email", channel, recipient = customer.Email, due });

                default:
                    return BadRequest(new { message = $"Unsupported channel '{channel}'. Use SMS, WHATSAPP or EMAIL." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment reminder to customer {CustomerId} via {Channel}", customerId, channel);
            return StatusCode(500, new { message = "Failed to send payment reminder" });
        }
    }

    /// <summary>Get the current notification channel settings.</summary>
    [HttpGet("settings")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var smsVal = await _settings.GetValueAsync(SmsKey, cancellationToken);
        var waVal = await _settings.GetValueAsync(WhatsAppKey, cancellationToken);
        var signalRRolesVal = await _settings.GetValueAsync(SignalRRolesKey, cancellationToken);

        return Ok(new NotificationSettingsDto
        {
            SmsEnabled = smsVal?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
            WhatsAppEnabled = waVal?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
            SignalRRoles = string.IsNullOrWhiteSpace(signalRRolesVal)
                ? []
                : [.. signalRRolesVal.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
        });
    }

    /// <summary>Enable or disable notification channels globally.</summary>
    [HttpPut("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateNotificationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        await _settings.SetValueAsync(SmsKey, request.SmsEnabled.ToString().ToLower(), "BOOL", NotifCategory, "Enable SMS notifications", true, cancellationToken);
        await _settings.SetValueAsync(WhatsAppKey, request.WhatsAppEnabled.ToString().ToLower(), "BOOL", NotifCategory, "Enable WhatsApp notifications", true, cancellationToken);
        await _settings.SetValueAsync(SignalRRolesKey, string.Join(",", request.SignalRRoles ?? []), "STRING", NotifCategory, "Roles that receive real-time SignalR notifications", true, cancellationToken);

        return Ok(new NotificationSettingsDto
        {
            SmsEnabled = request.SmsEnabled,
            WhatsAppEnabled = request.WhatsAppEnabled,
            SignalRRoles = request.SignalRRoles ?? []
        });
    }

    /// <summary>Get notification logs, optionally filtered by reference, channel, or status.</summary>
    [HttpGet("logs")]
    [HasPermission(Permissions.AuditView)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = HttpContext.RequestServices.GetRequiredService<AutoPartDbContext>();
            var q = db.NotificationLogs.Where(n => !n.Isdeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(channel)) q = q.Where(n => n.Channel == channel.ToUpper());
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(n => n.Status == status.ToUpper());

            var total = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderByDescending(n => n.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                data = items.Select(n => new
                {
                    n.Id,
                    n.Channel,
                    n.Recipient,
                    n.Status,
                    n.SentAt,
                    n.ErrorMessage,
                    n.CreatedDate
                }),
                pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification logs");
            return StatusCode(500, new { message = "Error retrieving notification logs" });
        }
    }

    // ── private ────────────────────────────────────────────────────────────

    private async Task<bool> IsEnabled(string key, CancellationToken ct)
    {
        var val = await _settings.GetValueAsync(key, ct);
        return val?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string BuildInvoiceHtml(SalesOrder order)
    {
        var rows = string.Concat(order.LineItems.Select(l =>
            $"<tr>" +
            $"<td style='padding:8px;border:1px solid #ddd'>{System.Net.WebUtility.HtmlEncode(l.Part?.Name ?? l.PartId.ToString())}</td>" +
            $"<td style='padding:8px;border:1px solid #ddd;text-align:center'>{l.Quantity}</td>" +
            $"<td style='padding:8px;border:1px solid #ddd;text-align:right'>{order.Currency} {l.UnitPrice:F2}</td>" +
            $"<td style='padding:8px;border:1px solid #ddd;text-align:right'>{order.Currency} {l.TotalPrice:F2}</td>" +
            $"</tr>"));

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><title>Invoice {order.SONumber}</title></head>
            <body style="font-family:Arial,sans-serif;max-width:640px;margin:auto;padding:24px;color:#333">
                <h2 style="margin:0 0 4px">Invoice — {System.Net.WebUtility.HtmlEncode(order.SONumber)}</h2>
                <p style="color:#666;margin:0 0 20px">Order Date: {order.SODate:dd MMM yyyy}</p>
                <p>Dear <strong>{System.Net.WebUtility.HtmlEncode(order.CustomerName)}</strong>,</p>
                <p>Thank you for your purchase. Here is your invoice summary.</p>
                <table style="width:100%;border-collapse:collapse;margin-top:16px">
                    <thead>
                        <tr style="background:#f5f5f5">
                            <th style="padding:8px;border:1px solid #ddd;text-align:left">Item</th>
                            <th style="padding:8px;border:1px solid #ddd;text-align:center">Qty</th>
                            <th style="padding:8px;border:1px solid #ddd;text-align:right">Unit Price</th>
                            <th style="padding:8px;border:1px solid #ddd;text-align:right">Total</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                    <tfoot>
                        <tr>
                            <td colspan="3" style="padding:8px;border:1px solid #ddd;text-align:right"><strong>Grand Total</strong></td>
                            <td style="padding:8px;border:1px solid #ddd;text-align:right"><strong>{order.Currency} {order.GrandTotal:F2}</strong></td>
                        </tr>
                    </tfoot>
                </table>
                <p style="margin-top:24px;color:#888;font-size:12px">This is an automated email. Please do not reply.</p>
            </body>
            </html>
            """;
    }
}
