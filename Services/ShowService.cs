using Backend_Nghiencf.Data;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend_Nghiencf.Services
{
    public class ShowService : IShowService
    {
        private readonly AppDbContext _context;

        public ShowService(AppDbContext context)
        {
            _context = context;
        }

        // Ép http -> https nếu cần, trim rỗng => null
        private static string? NormalizeBannerUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url.Substring("http://".Length);
            }
            return url;
        }

        // Chuẩn hoá DateTime về UTC (tránh lệch múi)
        private static DateTime NormalizeDate(DateTime date)
        {
            if (date.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);

            return date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();
        }

        private static ShowReadDto ToReadDto(Show s) => new ShowReadDto
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Date = s.Date,
            Location = s.Location,
            BannerUrl = NormalizeBannerUrl(s.BannerUrl), // trả về bản đã chuẩn hoá
            TotalSeats = s.TotalSeats,
            RemainingSeats = s.RemainingSeats,
            Slogan = s.Slogan,
            IsDefault = s.IsDefault,
            DeleteStatus = s.DeleteStatus
        };

        public async Task<IEnumerable<ShowReadDto>> GetAllAsync()
        {
            return await _context.Shows
                .Where(s => s.DeleteStatus == "Active")
                .OrderBy(s => s.Date)
                .Select(s => ToReadDto(s))
                .ToListAsync();
        }

        public async Task<ShowReadDto?> GetShowByTitleAsync(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            var show = await _context.Shows
                .FirstOrDefaultAsync(s =>
                    s.DeleteStatus == "Active" &&
                    s.Title.ToLower() == title.Trim().ToLower());

            return show == null ? null : ToReadDto(show);
        }

        public async Task<ShowReadDto> CreateAsync(ShowCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.TotalSeats == 0) throw new ArgumentException("Capacity phải >= 0", nameof(dto.TotalSeats));

            var show = new Show
            {
                Title = (dto.Title ?? string.Empty).Trim(),
                Description = (dto.Description ?? string.Empty).Trim(),
                Date = NormalizeDate(dto.Date),
                Location = (dto.Location ?? string.Empty).Trim(),
                BannerUrl = NormalizeBannerUrl(dto.BannerUrl), // dùng kết quả normalize
                TotalSeats = dto.TotalSeats,
                Slogan = (dto.Slogan ?? string.Empty).Trim(),
                // các cột IsDefault/DeleteStatus nếu DB mặc định thì không cần gán ở đây
            };

            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

            return ToReadDto(show);
        }

        public async Task<ShowReadDto?> UpdateAsync(string title, ShowUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            var show = await _context.Shows
                .FirstOrDefaultAsync(s =>
                    s.DeleteStatus == "Active" &&
                    s.Title.ToLower() == title.Trim().ToLower());

            if (show == null) return null;

            show.Title = (dto.Title ?? string.Empty).Trim();
            show.Description = (dto.Description ?? string.Empty).Trim();
            show.Date = NormalizeDate(dto.Date);
            show.Location = (dto.Location ?? string.Empty).Trim();
            show.TotalSeats = dto.TotalSeats;
            show.Slogan = (dto.Slogan ?? string.Empty).Trim();

            // BannerUrl: null = không đổi; "" = xoá; còn lại = cập nhật (đã normalize)
            if (dto.BannerUrl != null)
            {
                var normalized = NormalizeBannerUrl(dto.BannerUrl);
                show.BannerUrl = normalized; // nếu dto.BannerUrl rỗng => normalized = null => xoá
            }

            await _context.SaveChangesAsync();
            return ToReadDto(show);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var show = await _context.Shows.FirstOrDefaultAsync(s => s.Id == id);
            if (show == null) return false;

            show.DeleteStatus = "Deleted";
            if (show.IsDefault == "Active") show.IsDefault = "Inactive";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetDefaultShow(int id)
        {
            var showDefault = await _context.Shows.FirstOrDefaultAsync(s => s.Id == id && s.DeleteStatus == "Active");
            if (showDefault == null) return false;

            var currentDefault = await _context.Shows.FirstOrDefaultAsync(s => s.IsDefault == "Active");
            if (currentDefault != null) currentDefault.IsDefault = "Inactive";

            showDefault.IsDefault = "Active";
            await _context.SaveChangesAsync();
            return true;
        }

        // ====== Thêm cho FE: Coming Soon & Default ======

        // Lấy danh sách show sắp diễn ra (>= nowUtc), order theo thời gian, có thể giới hạn số lượng
        public async Task<IEnumerable<ShowReadDto>> GetComingSoonAsync(int take = 6)
        {
            var nowUtc = DateTime.UtcNow;
            return await _context.Shows
                .Where(s => s.DeleteStatus == "Active" && s.Date >= nowUtc)
                .OrderBy(s => s.Date)
                .Take(take)
                .Select(s => ToReadDto(s))
                .ToListAsync();
        }

        // Lấy show đang set default (nếu có)
        public async Task<ShowReadDto?> GetDefaultAsync()
        {
            var show = await _context.Shows
                .FirstOrDefaultAsync(s => s.DeleteStatus == "Active" && s.IsDefault == "Active");
            return show == null ? null : ToReadDto(show);
        }
    }
}
