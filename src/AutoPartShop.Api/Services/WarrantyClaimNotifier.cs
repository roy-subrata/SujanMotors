using System.Net;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Sends best-effort customer notifications for warranty-claim lifecycle events over the
/// channels the shop has enabled (SMS / WhatsApp / Email). Delivery is fire-and-forget: a
/// failed channel is logged and never blocks or rolls back the claim operation.
/// </summary>
public interface IWarrantyClaimNotifier
{
    Task ClaimReceivedAsync(WarrantyClaim claim, CancellationToken ct = default);
    Task ClaimApprovedAsync(WarrantyClaim claim, CancellationToken ct = default);
    Task ClaimRejectedAsync(WarrantyClaim claim, string reason, CancellationToken ct = default);
    Task ClaimCompletedAsync(WarrantyClaim claim, CancellationToken ct = default);
}

public class WarrantyClaimNotifier : IWarrantyClaimNotifier
{
    private readonly INotificationService _notifications;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<WarrantyClaimNotifier> _logger;
    private readonly string _shopName;

    public WarrantyClaimNotifier(
        INotificationService notifications,
        ICustomerRepository customerRepository,
        IConfiguration configuration,
        ILogger<WarrantyClaimNotifier> logger)
    {
        _notifications = notifications;
        _customerRepository = customerRepository;
        _logger = logger;
        _shopName = configuration["Smtp:FromName"] ?? "Auto Part Shop";
    }

    public Task ClaimReceivedAsync(WarrantyClaim claim, CancellationToken ct = default)
        => DispatchAsync(claim,
            name => $"Hi {name}, we've received your warranty claim {claim.ClaimNumber}. We'll keep you updated. - {_shopName}",
            $"Warranty claim {claim.ClaimNumber} received", ct);

    public Task ClaimApprovedAsync(WarrantyClaim claim, CancellationToken ct = default)
        => DispatchAsync(claim,
            name => $"Hi {name}, your warranty claim {claim.ClaimNumber} has been approved ({Humanize(claim.ServiceType)}). We'll proceed with the service. - {_shopName}",
            $"Warranty claim {claim.ClaimNumber} approved", ct);

    public Task ClaimRejectedAsync(WarrantyClaim claim, string reason, CancellationToken ct = default)
        => DispatchAsync(claim,
            name => $"Hi {name}, your warranty claim {claim.ClaimNumber} could not be approved. Reason: {reason}. Please contact us for details. - {_shopName}",
            $"Warranty claim {claim.ClaimNumber} update", ct);

    public Task ClaimCompletedAsync(WarrantyClaim claim, CancellationToken ct = default)
        => DispatchAsync(claim,
            name => $"Hi {name}, your warranty claim {claim.ClaimNumber} ({Humanize(claim.ServiceType)}) is complete and ready. Thank you! - {_shopName}",
            $"Warranty claim {claim.ClaimNumber} completed", ct);

    private async Task DispatchAsync(WarrantyClaim claim, Func<string, string> buildMessage, string emailSubject, CancellationToken ct)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(claim.CustomerId, ct);
            if (customer is null) return;

            var name = $"{customer.FirstName} {customer.LastName}".Trim();
            var message = buildMessage(string.IsNullOrWhiteSpace(name) ? "Customer" : name);

            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                await TrySendAsync(() => _notifications.SendSmsAsync(customer.Phone, message, ct));
                await TrySendAsync(() => _notifications.SendWhatsAppAsync(customer.Phone, message, ct));
            }

            if (!string.IsNullOrWhiteSpace(customer.Email))
                await TrySendAsync(() => _notifications.SendEmailAsync(
                    customer.Email, emailSubject, $"<p>{WebUtility.HtmlEncode(message)}</p>", ct));
        }
        catch (Exception ex)
        {
            // Never let notification problems surface to the caller / break the claim flow.
            _logger.LogWarning(ex, "Failed to notify customer for warranty claim {ClaimNumber}", claim.ClaimNumber);
        }
    }

    private async Task TrySendAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Warranty notification channel failed."); }
    }

    private static string Humanize(string serviceType) => serviceType?.ToUpperInvariant() switch
    {
        "REPAIR" => "repair",
        "REPLACEMENT" => "replacement",
        "REFUND" => "refund",
        _ => serviceType?.ToLowerInvariant() ?? "service"
    };
}
