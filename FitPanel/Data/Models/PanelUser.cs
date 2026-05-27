using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data
{

public class PanelUser : IdentityUser
{
     public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Coach profile details (null if user is Admin)
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
    public string? Specialization { get; set; }
    
    public string? InstagramUsername { get; set; }
    public string? InstagramLink { get; set; }
    
    public DateTime SubscriptionEndDate { get; set; } = DateTime.UtcNow.AddYears(1);
    public int MaxClients { get; set; } = 10;

    // One Coach → Many Clients
    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

}