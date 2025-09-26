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
            var show = new Show
            {
                Title = dto.Title,
                Description = dto.Description,
                Date = dto.Date,
                Location = dto.Location,
                BannerUrl = dto.BannerUrl,
                Capacity = dto.Capacity,
                Slogan = dto.Slogan
            };
            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

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

        public async Task<ShowReadDto?> UpdateAsync(string Title, ShowUpdateDto dto)
        {
            var show = await _context.Shows.FirstOrDefaultAsync(s => s.Title == Title);
            if (show == null) return null;

            show.Title = dto.Title;
            show.Description = dto.Description;
            show.Date = dto.Date;
            show.Location = dto.Location;
            show.BannerUrl = dto.BannerUrl;
            show.Capacity = dto.Capacity;
            show.Slogan = dto.Slogan;
            await _context.SaveChangesAsync();

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