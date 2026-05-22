namespace AutoPartShop.Application.DTOs.Notification;

public class NotificationSettingsDto
{
    public bool SmsEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public List<string> SignalRRoles { get; set; } = [];
}

public class UpdateNotificationSettingsRequest
{
    public bool SmsEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public List<string> SignalRRoles { get; set; } = [];
}
