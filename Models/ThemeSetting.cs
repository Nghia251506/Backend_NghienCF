// Models/ThemeSetting.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_Nghiencf.Models;

public sealed class ThemeSetting
{
    public int Id { get; set; }

    /// <summary>null = global; != null = theme riêng cho 1 show</summary>
    public int? ShowId { get; set; }

    public string PrimaryColor { get; set; } = "#f59e0b";
    public string Accent { get; set; } = "#ef4444";
    public string Background { get; set; } = "#0a0a0a";
    public string Surface { get; set; } = "#111827";
    public string Text { get; set; } = "#ffffff";
    public string Muted { get; set; } = "#9ca3af";
    public string Navbar { get; set; } = "#000000";
    public string ButtonFrom { get; set; } = "#f59e0b";
    public string ButtonTo { get; set; } = "#f97316";

    public string ScrollbarThumb { get; set; } = "#f59e0b";
    public string ScrollbarTrack { get; set; } = "#1f2937";

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
