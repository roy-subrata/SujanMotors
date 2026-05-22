using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AutoPartShop.Api.Hubs;

[Authorize]
public class SaleNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        foreach (var role in UserRoles())
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "staff");
        foreach (var role in UserRoles())
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role:{role}");
        await base.OnDisconnectedAsync(exception);
    }

    private IEnumerable<string> UserRoles() =>
        Context.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value) ?? [];
}
