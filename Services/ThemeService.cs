// Services/ThemeService.cs
using Backend_Nghiencf.Data;
using Backend_Nghiencf.Dtos;
using Backend_Nghiencf.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend_Nghiencf.Services;

public sealed class ThemeService : IThemeService
{
    private readonly AppDbContext _db;

    public ThemeService(AppDbContext db) => _db = db;

    private static ThemeSettingDto ToDto(ThemeSetting t) => new()
    {
        Id = t.Id, ShowId = t.ShowId,
        PrimaryColor = t.PrimaryColor, Accent = t.Accent, Background = t.Background,
        Surface = t.Surface, Text = t.Text, Muted = t.Muted, Navbar = t.Navbar,
        ButtonFrom = t.ButtonFrom, ButtonTo = t.ButtonTo,
        ScrollbarThumb = t.ScrollbarThumb, ScrollbarTrack = t.ScrollbarTrack,
    };

    public async Task<IReadOnlyList<ThemeSettingDto>> GetAllAsync(CancellationToken ct = default)
        => await _db.ThemeSettings
            .OrderBy(t => t.ShowId.HasValue).ThenBy(t => t.ShowId).ThenByDescending(t => t.UpdatedAt)
            .Select(t => ToDto(t)).ToListAsync(ct);

    public async Task<ThemeSettingDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => (await _db.ThemeSettings.FindAsync(new object?[] { id }, ct)) is ThemeSetting t ? ToDto(t) : null;

    public async Task<ThemeSettingDto?> GetActiveAsync(int? showId, CancellationToken ct = default)
    {
        // 1) theme theo show (nếu có) → 2) fallback global
        var t = await _db.ThemeSettings
            .Where(x => x.ShowId == showId)
            .OrderByDescending(x => x.UpdatedAt).FirstOrDefaultAsync(ct)
            ?? await _db.ThemeSettings.Where(x => x.ShowId == null)
                   .OrderByDescending(x => x.UpdatedAt).FirstOrDefaultAsync(ct);

        return t is null ? null : ToDto(t);
    }

    public async Task<ThemeSettingDto> CreateAsync(ThemeCreateDto dto, string? user, CancellationToken ct = default)
    {
        // enforce: mỗi show 1 bản ghi; global 1 bản ghi
        var exists = await _db.ThemeSettings.AnyAsync(t => t.ShowId == dto.ShowId, ct);
        if (exists) throw new InvalidOperationException(dto.ShowId is null
            ? "Global theme đã tồn tại."
            : $"ShowId {dto.ShowId} đã có theme.");

        var e = new ThemeSetting
        {
            ShowId = dto.ShowId,
            PrimaryColor = dto.PrimaryColor, Accent = dto.Accent, Background = dto.Background,
            Surface = dto.Surface, Text = dto.Text, Muted = dto.Muted, Navbar = dto.Navbar,
            ButtonFrom = dto.ButtonFrom, ButtonTo = dto.ButtonTo,
            ScrollbarThumb = dto.ScrollbarThumb, ScrollbarTrack = dto.ScrollbarTrack,
            CreatedBy = user, UpdatedBy = user,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.ThemeSettings.Add(e);
        await _db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task<ThemeSettingDto?> UpdateAsync(int id, ThemeUpdateDto dto, string? user, CancellationToken ct = default)
    {
        var e = await _db.ThemeSettings.FindAsync(new object?[] { id }, ct);
        if (e is null) return null;

        // nếu đổi ShowId -> cũng phải đảm bảo không trùng
        if (e.ShowId != dto.ShowId)
        {
            var exists = await _db.ThemeSettings.AnyAsync(t => t.ShowId == dto.ShowId && t.Id != id, ct);
            if (exists) throw new InvalidOperationException(dto.ShowId is null
                ? "Global theme đã tồn tại."
                : $"ShowId {dto.ShowId} đã có theme.");
        }

        e.ShowId = dto.ShowId;
        e.PrimaryColor = dto.PrimaryColor; e.Accent = dto.Accent; e.Background = dto.Background;
        e.Surface = dto.Surface; e.Text = dto.Text; e.Muted = dto.Muted; e.Navbar = dto.Navbar;
        e.ButtonFrom = dto.ButtonFrom; e.ButtonTo = dto.ButtonTo;
        e.ScrollbarThumb = dto.ScrollbarThumb; e.ScrollbarTrack = dto.ScrollbarTrack;
        e.UpdatedBy = user; e.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.ThemeSettings.FindAsync(new object?[] { id }, ct);
        if (e is null) return false;
        _db.ThemeSettings.Remove(e);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
