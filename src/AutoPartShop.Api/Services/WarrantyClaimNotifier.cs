using System.Net;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Sends best-effort customer notifications for warranty-claim lifecycle events over the
/// channels the shop has enabled (SMS / WhatsApp / Email). Delivery is fire-and-forget AND
/// non-blocking: messages are dispatched on a background task with a fresh DI scope, so a slow
/// or failing provider never delays the API response or rolls back the claim operation.
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WarrantyClaimNotifier> _logger;
    private readonly string _shopName;

    public WarrantyClaimNotifier(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<WarrantyClaimNotifier> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _shopName = configuration["Smtp:FromName"] ?? "Auto Part Shop";
    }

    public Task ClaimReceivedAsync(WarrantyClaim claim, CancellationToken ct = default)
        => DispatchInBackground(claim.CustomerId, claim.ClaimNumber,
            name => $"Hi {name}, we've received your warranty claim {claim.ClaimNumber}. We'll keep you updated. - {_shopName}",
            $"Warranty claim {claim.ClaimNumber} received");

    public Task ClaimApprovedAsync(WarrantyClaim claim, CancellationToken ct = default)
        => DispatchInBackground(claim.CustomerId, claim.ClaimNumber,
            name => $"Hi {name}, your warranty claim {claim.ClaimNumber} has been approved ({Humanize(claim.ServiceType)}). We'll proceed with the service. - {_shopName}",
            $"Warranty claim {claim.ClaimNumber} approved");

    public Task ClaimRejectedAsync(WarrantyClaim claim, string reason, CancellationToken ct = default)
        => DispatchInBackground(claim.CustomerId, claim.ClaimNumber,
            name => $"Hi {name}, your warranty claim {claim.ClaimNumber} could not be approved. Reason: {reason}. Please contact us for details. - {_shopName}",
            $"Warranty claim {claim.ClaimNumber} update");

    public Task ClaimCompletedAsync(WarrantyClaim claim, CancellationToken ct = default)
        => DispatchInBackground(claim.CustomerId, claim.ClaimNumber,
            name => $"Hi {name}, your warranty claim {claim.ClaimNumber} ({Humanize(claim.ServiceType)}) is complete and ready. Thank you! - {_shopName}",
            $"Warranty claim {claim.ClaimNumber} completed");

    /// <summary>
    /// Queues delivery on a background task with its own DI scope and returns immediately, so the
    /// request thread is never blocked on the SMS/WhatsApp/Email providers. The request-scoped
    /// CancellationToken is intentionally NOT used here — it would cancel once the response completes.
    /// </summary>
    private Task DispatchInBackground(Guid customerId, string claimNumber, Func<string, string> buildMessage, string emailSubject)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var customer = await customerRepository.GetByIdAsync(customerId);
                if (customer is null) return;

                var name = $"{customer.FirstName} {customer.LastName}".Trim();
                var message = buildMessage(string.IsNullOrWhiteSpace(name) ? "Customer" : name);

                if (!string.IsNullOrWhiteSpace(customer.Phone))
                {
                    await TrySendAsync(() => notifications.SendSmsAsync(customer.Phone, message));
                    await TrySendAsync(() => notifications.SendWhatsAppAsync(customer.Phone, message));
                }

                if (!string.IsNullOrWhiteSpace(customer.Email))
                    await TrySendAsync(() => notifications.SendEmailAsync(
                        customer.Email, emailSubject, $"<p>{WebUtility.HtmlEncode(message)}</p>"));
            }
            catch (Exception ex)
            {
                // Never let notification problems surface — this runs detached from the request.
                _logger.LogWarning(ex, "Failed to notify customer for warranty claim {ClaimNumber}", claimNumber);
            }
        });

        return Task.CompletedTask;
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
