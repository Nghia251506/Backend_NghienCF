using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;

namespace Backend_Nghiencf.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly TingeeClient _tingeeClient;
        private readonly ILogger<BookingService> _logger;

        public BookingService(AppDbContext context, ITingeeClient tingeeClient, ILogger<BookingService> logger)
        {
            _context = context;
            _tingeeClient = (TingeeClient?)tingeeClient;
            _logger = logger;
        }

        public async Task<IEnumerable<Booking>> GetAllSync()
        {
            return await _context.Bookings
                    .Select(e => new Booking
                    {
                        Id = e.Id,
                        ShowId = e.ShowId,
                        Show = e.Show,
                        TicketTypeId = e.TicketTypeId,
                        TicketType = e.TicketType,
                        CustomerName = e.CustomerName,
                        Phone = e.Phone,
                        Quantity = e.Quantity,
                        TotalAmount = e.TotalAmount,
                        PaymentStatus = e.PaymentStatus,
                        PaymentTime = e.PaymentTime
                    }).ToListAsync();
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingDto dto, CancellationToken ct = default)
        {
            if (dto.Quantity <= 0) throw new ArgumentException("Số lượng phải > 0");

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);

                // 1) Trừ kho atomic
                var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ticket_types
            SET remaining_quantity = remaining_quantity - {dto.Quantity}
            WHERE id = {dto.TicketTypeId} AND remaining_quantity >= {dto.Quantity}
        ", ct);

                if (affected == 0)
                {
                    var probe = await _context.TicketTypes.AsNoTracking()
                                 .Where(t => t.Id == dto.TicketTypeId)
                                 .Select(t => new { t.Id, t.RemainingQuantity })
                                 .SingleOrDefaultAsync(ct);

                    var remain = probe?.RemainingQuantity ?? 0;
                    throw new InvalidOperationException($"Không đủ số lượng vé (còn {remain}, yêu cầu {dto.Quantity}).");
                }

                // 2) Lấy type (NoTracking)
                var type = await _context.TicketTypes.AsNoTracking()
                            .SingleAsync(t => t.Id == dto.TicketTypeId, ct);

                // 3) Tạo booking
                _context.ChangeTracker.Clear();
                var booking = new Booking
                {
                    ShowId = type.ShowId,
                    TicketTypeId = dto.TicketTypeId,
                    CustomerName = dto.CustomerName?.Trim() ?? "",
                    Phone = dto.Phone?.Trim() ?? "",
                    Quantity = dto.Quantity,
                    TotalAmount = type.Price * dto.Quantity,
                    PaymentStatus = "pending",
                    PaymentTime = null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync(ct);

                // 4) Tạo QR
                TingeeQrResult qr;
                try
                {
                    qr = await _tingeeClient.CreateQrAsync(booking.Id, booking.TotalAmount, ct);
                }
                catch (HttpRequestException ex)
                {
                    // lỗi phía Tingee → rollback & trả lỗi 502 từ Controller
                    // rollback stock + xoá booking nếu cần (giữ như bạn đang làm)
                    throw;
                }

                return new BookingResponseDto
                {
                    BookingId = booking.Id,
                    TotalAmount = booking.TotalAmount,
                    PaymentQrUrl = qr.QrUrl,
                    PaymentQrImage = qr.QrCodeImage,
                    PaymentQrString = qr.QrCode
                };
            });
        }




        public async Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.PaymentStatus = "paid";
            booking.PaymentTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        public Task<BookingResponseDto> CreateBookingAsync(BookingDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
