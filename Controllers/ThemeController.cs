using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/theme")]
public sealed class ThemeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ThemeController> _log;
    public ThemeController(AppDbContext db, ILogger<ThemeController> log)
    {
        _db = db; _log = log;
    }

    [HttpGet]
    public async Task<ActionResult<ThemeDto>> Get([FromQuery] int? showId)
    {
        var q = _db.ThemeSettings.AsQueryable();
        var row = await q.Where(x => x.ShowId == showId).FirstOrDefaultAsync()
                  ?? await q.Where(x => x.ShowId == null).FirstOrDefaultAsync();
        if (row == null) return NotFound();

        return Ok(new ThemeDto {
            ShowId = row.ShowId,
            Primary = row.Primary,
            Accent = row.Accent,
            Background = row.Background,
            Surface = row.Surface,
            Text = row.Text,
            Muted = row.Muted,
            Navbar = row.Navbar,
            ButtonFrom = row.ButtonFrom,
            ButtonTo = row.ButtonTo,
            ScrollbarThumb = row.ScrollbarThumb,
            ScrollbarTrack = row.ScrollbarTrack
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ThemeDto dto)
    {
        // Nếu muốn: kiểm tra quyền admin ở đây
        var row = await _db.ThemeSettings.FirstOrDefaultAsync(x => x.ShowId == dto.ShowId)
               ?? new ThemeSetting { ShowId = dto.ShowId };

        row.Primary = dto.Primary;
        row.Accent = dto.Accent;
        row.Background = dto.Background;
        row.Surface = dto.Surface;
        row.Text = dto.Text;
        row.Muted = dto.Muted;
        row.Navbar = dto.Navbar;
        row.ButtonFrom = dto.ButtonFrom;
        row.ButtonTo = dto.ButtonTo;
        row.ScrollbarThumb = dto.ScrollbarThumb;
        row.ScrollbarTrack = dto.ScrollbarTrack;
        row.UpdatedAt = DateTime.UtcNow;

        if (row.Id == 0) _db.ThemeSettings.Add(row);
        await _db.SaveChangesAsync();
        return Ok(new { code = "00" });
    }
}
