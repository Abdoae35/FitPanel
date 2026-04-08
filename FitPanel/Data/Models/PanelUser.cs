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
}

}