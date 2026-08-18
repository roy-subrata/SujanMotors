using AutoPartShop.Api.Authorization;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;
[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class InboxNotificationsController(IInboxNotificationRepository _repository, ILogger<InboxNotificationsController> _logger)
    : ControllerBase
{
    /// <summary>Paged inbox notifications (reorder alerts, etc.), newest first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? type = null,
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (items, totalCount) = await _repository.GetPagedAsync(type, unreadOnly, page, pageSize, cancellationToken);
            var data = items.Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.IsRead,
                n.ReadAt,
                n.RouterLink,
                n.QueryParamsJson,
                n.PayloadJson,
                n.CreatedDate
            });

            return Ok(new
            {
                data,
                pagination = new { page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inbox notifications");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving inbox notifications" });
        }
    }

    /// <summary>Number of unread inbox notifications (used for the bell badge).</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var count = await _repository.GetUnreadCountAsync(type, cancellationToken);
        return Ok(new { count });
    }

    /// <summary>Mark an inbox notification as read (or unread when isRead=false).</summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, [FromBody] MarkInboxReadRequest? request = null, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(id, cancellationToken);
        if (notification is null) return NotFound(new { message = "Inbox notification not found" });

        if (request?.IsRead == false)
            notification.MarkAsUnread();
        else
            notification.MarkAsRead();

        await _repository.UpdateAsync(notification, cancellationToken);
        return Ok(new { id = notification.Id, isRead = notification.IsRead });
    }

    /// <summary>Mark every unread notification as read (optionally scoped to a type).</summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead([FromQuery] string? type = null, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.MarkAllAsReadAsync(type, cancellationToken);
        return Ok(new { updated });
    }
}

public class MarkInboxReadRequest
{
    public bool IsRead { get; set; } = true;
}
