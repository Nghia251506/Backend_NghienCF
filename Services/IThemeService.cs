// Services/IThemeService.cs
using Backend_Nghiencf.Dtos;

namespace Backend_Nghiencf.Services;

public interface IThemeService
{
    Task<IReadOnlyList<ThemeSettingDto>> GetAllAsync(CancellationToken ct = default);
    Task<ThemeSettingDto?> GetByIdAsync(int id, CancellationToken ct = default);
    /// <summary>Lấy theme đang áp dụng cho show; nếu không có → trả global (ShowId=null).</summary>
    Task<ThemeSettingDto?> GetActiveAsync(int? showId, CancellationToken ct = default);

    Task<ThemeSettingDto> CreateAsync(ThemeCreateDto dto, string? user, CancellationToken ct = default);
    Task<ThemeSettingDto?> UpdateAsync(int id, ThemeUpdateDto dto, string? user, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

