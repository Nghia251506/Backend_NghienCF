using Backend_Nghiencf.Data;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;
using Microsoft.EntityFrameworkCore;
namespace Backend_Nghiencf.Services
{
    public class SettingService : ISettingService
    {
        private readonly AppDbContext _context;

        public SettingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SettingReadDto>> GetAllAsync()
        {
            return await _context.Settings
                .Select(s => new SettingReadDto
                {
                    Id = s.Id,
                    SettingKey = s.SettingKey,
                    SettingValue = s.SettingValue,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<SettingReadDto?> GetByKeyAsync(string key)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting == null) return null;

            return new SettingReadDto
            {
                Id = setting.Id,
                SettingKey = setting.SettingKey,
                SettingValue = setting.SettingValue,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<SettingReadDto> CreateAsync(SettingCreateDto dto)
        {
            var setting = new Setting
            {
                SettingKey = dto.SettingKey,
                SettingValue = dto.SettingValue
            };

            _context.Settings.Add(setting);
            await _context.SaveChangesAsync();

            return new SettingReadDto
            {
                Id = setting.Id,
                SettingKey = setting.SettingKey,
                SettingValue = setting.SettingValue,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<SettingReadDto?> UpdateAsync(string key, SettingUpdateDto dto)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting == null) return null;

            setting.SettingValue = dto.SettingValue;
            await _context.SaveChangesAsync();

            return new SettingReadDto
            {
                Id = setting.Id,
                SettingKey = setting.SettingKey,
                SettingValue = setting.SettingValue,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<bool> DeleteAsync(string key)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting == null) return false;

            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}