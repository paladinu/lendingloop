using Api.Models;

namespace Api.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string userId, NotificationType type, 
        string message, string? itemId = null, string? itemRequestId = null, 
        string? relatedUserId = null);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 50);
    Task<int> GetUnreadCountAsync(string userId);
    Task<Notification?> MarkAsReadAsync(string notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(string notificationId, string userId);
}
