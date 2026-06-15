using System.ComponentModel.DataAnnotations;

namespace FitPanel.Data.Models;

public class CoachWarmUpDictionary
{
    public int Id { get; set; }

    // Every coach owns their own warm-up entries
    [Required]
    public string CoachId { get; set; }
    public PanelUser Coach { get; set; }

    [Required]
    public string WarmUpName { get; set; }    // e.g. "Push Day Warm Up"

    public string? WarmUpLink { get; set; }   // e.g. https://youtube.com/...
}
