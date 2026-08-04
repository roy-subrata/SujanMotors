using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Domain.Entities;

public class NotificationLog : AuditableEntity
{
    public NotificationChannel Channel { get; private set; }
    public string Recipient { get; private set; } = string.Empty; // phone number or email address
    public string Message { get; private set; } = string.Empty;
    public NotificationLogStatus Status { get; private set; } = NotificationLogStatus.PENDING;
    public string? ErrorMessage { get; private set; }
    public DateTime? SentAt { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(NotificationChannel channel, string recipient, string message)
    {
        return new NotificationLog
        {
            Channel = channel,
            Recipient = recipient,
            Message = message,
            Status = NotificationLogStatus.PENDING,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Isdeleted = false
        };
    }

    public void MarkSent()
    {
        Status = NotificationLogStatus.SENT;
        SentAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = NotificationLogStatus.FAILED;
        ErrorMessage = error;
        ModifiedDate = DateTime.UtcNow;
    }
}
