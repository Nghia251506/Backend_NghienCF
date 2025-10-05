public sealed class ThemeSetting
{
    public int Id { get; set; }
    // scope cho mở rộng đa site hoặc theo show
    public int? ShowId { get; set; } // null = global
    public string Primary { get; set; } = "#f59e0b"; // vàng
    public string Accent { get; set; } = "#ef4444";  // đỏ
    public string Background { get; set; } = "#0a0a0a"; // nền
    public string Surface { get; set; } = "#111827"; // card
    public string Text { get; set; } = "#ffffff";
    public string Muted { get; set; } = "#9ca3af";
    public string Navbar { get; set; } = "#000000";
    public string ButtonFrom { get; set; } = "#f59e0b";
    public string ButtonTo { get; set; } = "#f97316";

    // Scrollbar
    public string ScrollbarThumb { get; set; } = "#f59e0b";
    public string ScrollbarTrack { get; set; } = "#1f2937";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
