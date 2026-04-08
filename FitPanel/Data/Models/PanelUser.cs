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

    // One Coach → Many Clients
    public ICollection<Client> Clients { get; set; } = new List<Client>();
}

}