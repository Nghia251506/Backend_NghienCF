using System.Data;
using Backend_Nghiencf.Data;           // AppDbContext
using Backend_Nghiencf.Dtos.Ticket;
using Backend_Nghiencf.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Backend_Nghiencf.Services;

public sealed class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TicketService> _log;

    public TicketService(AppDbContext context, ILogger<TicketService> log)
    {
        _context = context;
        _log = log;
    }

    public async Task<TicketReadDto> CreateAsync(int bookingId, DateTime? eventDate, CancellationToken ct = default)
    {
        // Dùng connection của DbContext
        var conn = (MySqlConnection)_context.Database.GetDbConnection();

        // Chuẩn bị command gọi stored procedure
        await using var cmd = new MySqlCommand("sp_add_ticket", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        // IN params
        cmd.Parameters.Add(new MySqlParameter("p_booking_id", MySqlDbType.Int32) { Value = bookingId });
        // MySQL DATE -> chỉ lấy phần Date; nếu null thì để DBNull (SP sẽ dùng CURDATE())
        cmd.Parameters.Add(new MySqlParameter("p_event_date", MySqlDbType.Date)
        {
            Value = eventDate?.Date ?? (object)DBNull.Value
        });

        // OUT params
        var pId = new MySqlParameter("p_ticket_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
        var pCode = new MySqlParameter("p_ticket_code", MySqlDbType.VarChar, 128) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pId);
        cmd.Parameters.Add(pCode);

        var mustClose = conn.State != ConnectionState.Open;
        try
        {
            if (mustClose) await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);

            var newId = (pId.Value is int i) ? i : Convert.ToInt32(pId.Value);
            // đọc lại ticket từ DB
            var t = await _context.Tickets.AsNoTracking()
                .Where(x => x.Id == newId)
                .Select(x => new TicketReadDto
                {
                    Id         = x.Id,
                    BookingId  = x.BookingId,
                    TicketCode = x.TicketCode,
                    Status     = x.Status,
                    IssuedAt   = x.IssuedAt
                })
                .SingleAsync(ct);

            return t;
        }
        catch (MySqlException ex)
        {
            // ví dụ lỗi SIGNAL trong SP (trùng code sau 100 lần thử)
            _log.LogError(ex, "Create ticket failed for booking {BookingId}", bookingId);
            throw new InvalidOperationException(ex.Message, ex);
        }
        finally
        {
            if (mustClose) await conn.CloseAsync();
        }
    }

    public async Task<TicketReadDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Tickets.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TicketReadDto
            {
                Id         = x.Id,
                BookingId  = x.BookingId,
                TicketCode = x.TicketCode,
                Status     = x.Status,
                IssuedAt   = x.IssuedAt
            })
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<TicketReadDto>> GetByBookingAsync(int bookingId, CancellationToken ct = default)
    {
        return await _context.Tickets.AsNoTracking()
            .Where(x => x.BookingId == bookingId)
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => new TicketReadDto
            {
                Id         = x.Id,
                BookingId  = x.BookingId,
                TicketCode = x.TicketCode,
                Status     = x.Status,
                IssuedAt   = x.IssuedAt
            })
            .ToListAsync(ct);
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, CancellationToken ct = default)
    {
        // tùy bạn validate status thuộc {valid, used, invalid,...}
        var t = await _context.Tickets.FindAsync(new object?[] { id }, ct);
        if (t == null) return false;

        t.Status = status.Trim();
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
