// Services/NotificationServices/NotificationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitPanel.Data;
using FitPanel.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Services;

public class NotificationService : INotificationService
{
    private readonly FitPanelDbContext _db;

    public NotificationService(FitPanelDbContext db)
    {
        _db = db;
    }

    public async Task<List<Notification>> GetNotificationsForUserAsync(string userId)
    {
        try
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        catch (Exception)
        {
            // Fallback if migration hasn't been run yet to prevent app crash
            return new List<Notification>();
        }
    }

    public async Task CreateNotificationAsync(string userId, string title, string message)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Silent fallback if table is not created yet
        }
    }

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        try
        {
            var notif = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif != null)
            {
                notif.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        try
        {
            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception)
        {
        }
    }

    public async Task DeleteNotificationAsync(int notificationId, string userId)
    {
        try
        {
            var notif = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif != null)
            {
                _db.Notifications.Remove(notif);
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
        }
    }
}
