using System.Runtime.Intrinsics.X86;
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

        public async Task<IEnumerable<ShowReadDto>> GetAllAsync()
        {
            return await _context.Shows
            .Select(s => new ShowReadDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Date = s.Date,
                Location = s.Location,
                BannerUrl = s.BannerUrl,
                Capacity = s.Capacity,
                Slogan = s.Slogan
            })
            .ToListAsync();
        }

        public async Task<ShowReadDto?> GetShowByTitleAsync(string Title)
        {
            var show = await _context.Shows.FirstOrDefaultAsync(s => s.Title == Title);
            if (show == null) return null;

            return new ShowReadDto
            {
                Id = show.Id,
                Title = show.Title,
                Description = show.Description,
                Date = show.Date,
                Location = show.Location,
                BannerUrl = show.BannerUrl,
                Capacity = show.Capacity,
                Slogan = show.Slogan
            };
        }


        public async Task<ShowReadDto> CreateAsync(ShowCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Capacity == "") throw new ArgumentException("Capacity phải >= 0", nameof(dto.Capacity));

            var show = new Show
            {
                Title = (dto.Title ?? string.Empty).Trim(),
                Description = (dto.Description ?? string.Empty).Trim(),
                Date = NormalizeDate(dto.Date),
                Location = (dto.Location ?? string.Empty).Trim(),
                BannerUrl = string.IsNullOrWhiteSpace(dto.BannerUrl) ? null : dto.BannerUrl!.Trim(),
                Capacity = dto.Capacity,
                Slogan = (dto.Slogan ?? string.Empty).Trim()
            };

            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

            return ToReadDto(show);
        }

        private DateTime NormalizeDate(DateTimeOffset date)
        {
            throw new NotImplementedException();
        }

        public async Task<ShowReadDto?> UpdateAsync(string Title, ShowUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(Title)) return null;

            // Tìm theo Title không phân biệt hoa/thường
            var show = await _context.Shows
                .FirstOrDefaultAsync(s => s.Title.ToLower() == Title.Trim().ToLower());

            if (show == null) return null;

            // Cập nhật các trường (trim & chuẩn hoá)
            show.Title = (dto.Title ?? string.Empty).Trim();
            show.Description = (dto.Description ?? string.Empty).Trim();
            show.Date = NormalizeDate(dto.Date);
            show.Location = (dto.Location ?? string.Empty).Trim();
            show.Capacity = dto.Capacity;
            show.Slogan = (dto.Slogan ?? string.Empty).Trim();

            // BannerUrl theo 3 trạng thái:
            // - null  => không đổi (FE không gửi)
            // - ""    => xoá ảnh (set null)
            // - value => cập nhật URL
            if (dto.BannerUrl != null)
            {
                show.BannerUrl = string.IsNullOrWhiteSpace(dto.BannerUrl)
                    ? null
                    : dto.BannerUrl.Trim();
            }

            await _context.SaveChangesAsync();

            return ToReadDto(show);
        }

        // ----------------- helpers -----------------

        private static ShowReadDto ToReadDto(Show s) => new ShowReadDto
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Date = s.Date,
            Location = s.Location,
            BannerUrl = s.BannerUrl,
            Capacity = s.Capacity,
            Slogan = s.Slogan
        };

        // Nếu Date là DateTime → chuẩn hoá về UTC để khớp FE gửi ISO.
        // Nếu bạn dùng DateTimeOffset thì có thể trả về thẳng không cần Normalize.
        private static DateTime NormalizeDate(DateTime date)
        {
            // Nếu Kind = Unspecified (thường khi map từ JSON) → coi như UTC để tránh lệch múi.
            if (date.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);

            return date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var show = await _context.Shows.FirstOrDefaultAsync(s => s.Id == id);
            if (show == null) return false;

            _context.Shows.Remove(show);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}