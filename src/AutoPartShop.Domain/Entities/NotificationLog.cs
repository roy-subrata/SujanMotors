namespace AutoPartShop.Domain.Entities;

public class NotificationLog : AuditableEntity
{
    public string Channel { get; private set; } = string.Empty;   // SMS | EMAIL | WHATSAPP | SIGNALR
    public string Recipient { get; private set; } = string.Empty; // phone number or email address
    public string Message { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PENDING";       // PENDING | SENT | FAILED
    public string? ErrorMessage { get; private set; }
    public DateTime? SentAt { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(string channel, string recipient, string message)
    {
        return new NotificationLog
        {
            Channel = channel,
            Recipient = recipient,
            Message = message,
            Status = "PENDING",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Isdeleted = false
        };
    }

    public void MarkSent()
    {
        Status = "SENT";
        SentAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = "FAILED";
        ErrorMessage = error;
        ModifiedDate = DateTime.UtcNow;
    }
}
