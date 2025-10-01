// Services/BookingDevService.cs
using Backend_Nghiencf.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend_Nghiencf.Services;

public sealed class BookingDevService : IBookingDevService
{
    private readonly AppDbContext _db;
    private readonly ITicketService _ticketSvc;
    private readonly ILogger<BookingDevService> _log;

    public BookingDevService(AppDbContext db, ITicketService ticketSvc, ILogger<BookingDevService> log)
    {
        _db = db; _ticketSvc = ticketSvc; _log = log;
    }

    public async Task<int> MarkPaidAndIssueTicketsAsync(int bookingId, CancellationToken ct = default)
    {
        var b = await _db.Bookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == bookingId, ct);
        if (b == null) throw new InvalidOperationException($"Booking {bookingId} not found.");

        if (!string.Equals(b.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            return 0; // chưa paid thì bỏ qua

        var issued = await _db.Tickets.CountAsync(t => t.BookingId == b.Id, ct);
        var need = Math.Max(0, b.Quantity - issued);
        if (need == 0) return 0;

        // có thể truyền ngày show nếu bạn muốn mã vé theo ngày sự kiện
        DateTime? eventDate = null;

        for (int i = 0; i < need; i++)
            await _ticketSvc.CreateAsync(b.Id, eventDate, ct);

        return need;
    }
}
