// Data/Models/Notification.cs
using System;

namespace FitPanel.Data.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Recipient (Coach ID)
    public PanelUser User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
