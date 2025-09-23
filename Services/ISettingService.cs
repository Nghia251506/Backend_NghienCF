using Backend_Nghiencf.DTOs;

namespace Backend_Nghiencf.Services
{
    public interface ISettingService
    {
        Task<IEnumerable<SettingReadDto>> GetAllAsync();
        Task<SettingReadDto?> GetByKeyAsync(string key);
        Task<SettingReadDto> CreateAsync(SettingCreateDto dto);
        Task<SettingReadDto?> UpdateAsync(string key, SettingUpdateDto dto);
        Task<bool> DeleteAsync(string key);
    }

    
}
