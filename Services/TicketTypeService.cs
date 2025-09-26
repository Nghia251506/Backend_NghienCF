using System.Runtime.Intrinsics.X86;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;
using Microsoft.EntityFrameworkCore;
namespace Backend_Nghiencf.Services
{
    public class TicketTypeService : ITicketTypeService
    {
        private readonly AppDbContext _context;

        public TicketTypeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TicketTypeReadDto>> GetAllAsync()
        {
            return await _context.TicketTypes
            .Select(tp => new TicketTypeReadDto
            {
                Id = tp.Id,
                ShowId = tp.ShowId,
                Show = tp.Show,
                Name = tp.Name,
                Color = tp.Color,
                Price = tp.Price,
                TotalQuantity = tp.TotalQuantity,
                RemainingQuantity = tp.RemainingQuantity
            }).ToListAsync();
        }

        public async Task<TicketTypeReadDto?> GetTypeById(int id)
        {
            var s = await _context.TicketTypes
                .Include(x => x.Show)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s == null) return null;

            return new TicketTypeReadDto
            {
                Id = s.Id,
                ShowId = s.ShowId,
                Show = s.Show,
                Name = s.Name,
                Color = s.Color,
                Price = s.Price,
                TotalQuantity = s.TotalQuantity,
                RemainingQuantity = s.RemainingQuantity
            };
        }

        public async Task<TicketTypeReadDto> CreateAsync(TicketTypeCreateDto dto)
        {
            var s = new TicketType
            {
                ShowId = dto.ShowId,
                Name = dto.Name,
                Color = dto.Color,
                Price = dto.Price,
                TotalQuantity = dto.TotalQuantity,
                RemainingQuantity = dto.RemainingQuantity
            };
            _context.TicketTypes.Add(s);
            await _context.SaveChangesAsync();
            return (await GetTypeById(s.Id))!;
        }

        public async Task<bool> UpdateAsync(int id, TicketTypeUpdateDto dto)
        {
            var s = await _context.TicketTypes.FindAsync(id);
            if (s == null) return false;

            s.ShowId = dto.ShowId;
            s.Name = dto.Name;
            s.Color = dto.Color;
            s.Price = dto.Price;
            s.TotalQuantity = dto.TotalQuantity;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var s = await _context.TicketTypes.FindAsync(id);
            if (s == null) return false;
            _context.TicketTypes.Remove(s);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}