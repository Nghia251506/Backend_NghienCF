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

        // Tiện ích tính available
        private static int ComputeAvailable(int? showRemainingSeats, int? showTotalSeats, int seatUnit, int totalQuantity)
        {
            var remainingSeats = showRemainingSeats ?? showTotalSeats ?? 0;
            var unit = seatUnit <= 0 ? 1 : seatUnit;

            // available nếu chỉ tính theo kho dùng chung
            var byShared = remainingSeats / unit;

            // nếu muốn áp trần riêng của combo (totalQuantity), thì lấy min
            if (totalQuantity > 0 && totalQuantity != int.MaxValue)
                return Math.Max(0, Math.Min(byShared, totalQuantity));

            return Math.Max(0, byShared);
        }

        public async Task<List<TicketTypeReadDto>> GetByShowAsync(int showId, CancellationToken ct = default)
        {
            // Join với shows để tính available từ remaining_seats
            var list = await (from t in _context.TicketTypes.AsNoTracking()
                              join s in _context.Shows.AsNoTracking() on t.ShowId equals s.Id
                              where t.ShowId == showId && s.DeleteStatus == "Active"
                              orderby t.Price
                              select new
                              {
                                  T = t,
                                  S = s
                              }).ToListAsync(ct);

            return list.Select(x => new TicketTypeReadDto
            {
                Id = x.T.Id,
                ShowId = x.T.ShowId,
                Show = x.T.Show, // có thể bỏ nếu payload to
                Name = x.T.Name,
                Color = x.T.Color,
                Price = x.T.Price,
                Description = x.T.Description,
                SeatUnit = (int)x.T.SeatsUnit,
                TotalQuantity = x.T.TotalQuantity,
                RemainingQuantity = x.T.RemainingQuantity, // giữ để tương thích
                Available = ComputeAvailable((int?)x.S.RemainingSeats, x.S.TotalSeats, (int)x.T.SeatsUnit, x.T.TotalQuantity)
            }).ToList();
        }

        public async Task<IEnumerable<TicketTypeReadDto>> GetAllAsync()
        {
            // Lấy kèm show để tính Available nhất quán
            var list = await (from t in _context.TicketTypes.AsNoTracking()
                              join s in _context.Shows.AsNoTracking() on t.ShowId equals s.Id
                              where s.DeleteStatus == "Active"
                              orderby t.ShowId, t.Price
                              select new
                              {
                                  T = t,
                                  S = s
                              }).ToListAsync();

            return list.Select(x => new TicketTypeReadDto
            {
                Id = x.T.Id,
                ShowId = x.T.ShowId,
                Show = x.T.Show,
                Name = x.T.Name,
                Color = x.T.Color,
                Price = x.T.Price,
                Description = x.T.Description,
                SeatUnit = (int)x.T.SeatsUnit,
                TotalQuantity = x.T.TotalQuantity,
                RemainingQuantity = x.T.RemainingQuantity,
                Available = ComputeAvailable((int?)x.S.RemainingSeats, x.S.TotalSeats, (int)x.T.SeatsUnit, x.T.TotalQuantity)
            });
        }

        public async Task<TicketTypeReadDto?> GetTypeById(int id)
        {
            var t = await _context.TicketTypes
                .Include(x => x.Show)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (t == null || t.Show == null || t.Show.DeleteStatus != "Active")
                return null;

            return new TicketTypeReadDto
            {
                Id = t.Id,
                ShowId = t.ShowId,
                Show = t.Show,
                Name = t.Name,
                Color = t.Color,
                Price = t.Price,
                Description = t.Description,
                SeatUnit = (int)t.SeatsUnit,
                TotalQuantity = t.TotalQuantity,
                RemainingQuantity = t.RemainingQuantity,
                Available = ComputeAvailable((int?)t.Show.RemainingSeats, t.Show.TotalSeats, (int)t.SeatsUnit, t.TotalQuantity)
            };
        }

        public async Task<TicketTypeReadDto> CreateAsync(TicketTypeCreateDto dto)
        {
            var unit = dto.SeatUnit <= 0 ? 1 : dto.SeatUnit;

            var t = new TicketType
            {
                ShowId = dto.ShowId,
                Name = dto.Name,
                Color = dto.Color,
                Price = dto.Price,
                Description = dto.Description,
                SeatsUnit = unit,                        // 👈 set seat unit
                TotalQuantity = dto.TotalQuantity,
                // Không đụng remaining_quantity: để tương thích, có thể set = total ở thời điểm khởi tạo
                RemainingQuantity = dto.TotalQuantity
            };
            _context.TicketTypes.Add(t);
            await _context.SaveChangesAsync();

            // Lấy lại kèm show để trả Available
            var saved = await _context.TicketTypes.Include(x => x.Show).FirstAsync(x => x.Id == t.Id);
            return new TicketTypeReadDto
            {
                Id = saved.Id,
                ShowId = saved.ShowId,
                Show = saved.Show,
                Name = saved.Name,
                Color = saved.Color,
                Price = saved.Price,
                Description = saved.Description,
                SeatUnit = (int)saved.SeatsUnit,
                TotalQuantity = saved.TotalQuantity,
                RemainingQuantity = saved.RemainingQuantity,
                Available = ComputeAvailable((int?)(saved.Show?.RemainingSeats), saved.Show?.TotalSeats, (int)saved.SeatsUnit, saved.TotalQuantity)
            };
        }

        public async Task<bool> UpdateAsync(int id, TicketTypeUpdateDto dto)
        {
            var t = await _context.TicketTypes.FindAsync(id);
            if (t == null) return false;

            t.ShowId = dto.ShowId;
            t.Name = dto.Name;
            t.Color = dto.Color;
            t.Price = dto.Price;
            t.Description = dto.Description;
            t.SeatsUnit = dto.SeatUnit <= 0 ? 1 : dto.SeatUnit;

            // ⚠️ Không reset RemainingQuantity về TotalQuantity nữa
            // vì available bây giờ tính theo show.remaining_seats.
            t.TotalQuantity = dto.TotalQuantity;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var t = await _context.TicketTypes.FindAsync(id);
            if (t == null) return false;
            _context.TicketTypes.Remove(t);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
