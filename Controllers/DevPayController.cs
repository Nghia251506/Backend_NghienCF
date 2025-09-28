using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;          // đổi namespace đúng solution của bạn
using Backend_Nghiencf.Models;        // chứa Booking, Ticket
using MySqlConnector;

namespace Backend_Nghiencf.Controllers
{
    [ApiController]
    [Route("api")]
    public class DevController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHostEnvironment _env;

        public DevController(AppDbContext context, IHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// DEV ONLY: Ép booking -> paid & phát hành vé (gọi SP sp_add_ticket)
        /// POST /api/dev-pay/{bookingId}
        /// </summary>
        [HttpPost("dev-pay/{bookingId:long}")]
        public async Task<IActionResult> ForcePay(long bookingId, CancellationToken ct)
        {
            // Chỉ cho phép ở Development (tránh lộ ra Production)
            if (!_env.IsDevelopment())
                return NotFound();

            // Lấy booking
            var booking = await _context.Bookings
                .SingleOrDefaultAsync(b => b.Id == bookingId, ct);

            if (booking == null)
                return NotFound(new { message = "Booking not found" });

            // Nếu chưa paid -> set paid
            if (!string.Equals(booking.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                booking.PaymentStatus = "paid";
                booking.PaymentTime = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            // Đếm số vé hiện tại của booking (để sau lấy đúng vé vừa tạo)
            var countBefore = await _context.Tickets
                .Where(t => t.BookingId == booking.Id)
                .CountAsync(ct);

            // Gọi SP phát hành đủ Quantity vé
            // SP: sp_add_ticket(IN p_booking_id INT, IN p_event_date DATE, OUT p_ticket_id INT, OUT p_ticket_code VARCHAR(128))
            var conn = _context.Database.GetDbConnection();
            await using (conn)
            {
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync(ct);

                for (int i = 0; i < booking.Quantity; i++)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "sp_add_ticket";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var pBookingId = new MySqlParameter("p_booking_id", MySqlDbType.Int32) { Value = booking.Id };
                    var pEventDate = new MySqlParameter("p_event_date", MySqlDbType.Date) { Value = DBNull.Value }; // NULL -> dùng CURDATE() trong SP
                    var pTicketId = new MySqlParameter("p_ticket_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                    var pTicketCode = new MySqlParameter("p_ticket_code", MySqlDbType.VarChar, 128) { Direction = ParameterDirection.Output };

                    cmd.Parameters.AddRange(new[] { pBookingId, pEventDate, pTicketId, pTicketCode });

                    await cmd.ExecuteNonQueryAsync(ct);
                    // Nếu cần đọc ticketId / ticketCode từng vé, có thể lấy từ pTicketId.Value / pTicketCode.Value
                }
            }

            // Lấy danh sách vé mới tạo (skip số vé cũ)
            var newTickets = await _context.Tickets
                .Where(t => t.BookingId == booking.Id)
                .OrderBy(t => t.Id)
                .Skip(countBefore)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    Status = t.Status,
                    IssuedAt = t.IssuedAt
                })
                .ToListAsync(ct);

            return Ok(new
            {
                ok = true,
                bookingId = booking.Id,
                issued = newTickets.Count,
                tickets = newTickets
            });
        }

        // DTO trả về cho FE (gọn nhẹ)
        public sealed class TicketDto
        {
            public int Id { get; set; }
            public int BookingId { get; set; }
            public string TicketCode { get; set; } = string.Empty;
            public string Status { get; set; } = "valid";
            public DateTime IssuedAt { get; set; }
        }
    }
}
