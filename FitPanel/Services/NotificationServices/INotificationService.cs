// Services/NotificationServices/INotificationService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using FitPanel.Data.Models;

namespace FitPanel.Services;

public interface INotificationService
{
    Task<List<Notification>> GetNotificationsForUserAsync(string userId);
    Task CreateNotificationAsync(string userId, string title, string message);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task DeleteNotificationAsync(int notificationId, string userId);
}
