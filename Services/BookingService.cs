using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;
using System.Data;
using MySqlConnector;

namespace Backend_Nghiencf.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly ITingeeClient _tingeeClient;
        private readonly ILogger<BookingService> _logger;

        public BookingService(AppDbContext context, ITingeeClient tingeeClient, ILogger<BookingService> logger)
        {
            _context = context;
            _tingeeClient = tingeeClient; // dùng interface, bỏ cast
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
                    PaymentTime = e.PaymentTime,
                    SeatsConsumed = e.SeatsConsumed
                })
                .ToListAsync();
        }

        // PROC MỚI: sp_create_booking_v2
        public async Task<BookingResponseDto> CreateBookingAsync(BookingDto dto, CancellationToken ct = default)
        {
            if (dto.Quantity <= 0) throw new ArgumentException("Số lượng phải > 0");

            await using var conn = (MySqlConnection)_context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            await using var cmd = new MySqlCommand("sp_create_booking_v2", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // ===== INPUT =====
            cmd.Parameters.AddWithValue("@p_show_id", dto.ShowId);
            cmd.Parameters.AddWithValue("@p_ticket_type_id", dto.TicketTypeId);
            cmd.Parameters.AddWithValue("@p_customer_name", dto.CustomerName?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@p_phone", dto.Phone?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@p_quantity", dto.Quantity);

            // ===== OUTPUT =====
            var pBookingId   = new MySqlParameter("@p_booking_id",  MySqlDbType.Int64)     { Direction = ParameterDirection.Output };
            var pPaymentRef  = new MySqlParameter("@p_payment_ref", MySqlDbType.VarChar,64){ Direction = ParameterDirection.Output };
            var pTotalAmount = new MySqlParameter("@p_total_amount",MySqlDbType.NewDecimal){ Direction = ParameterDirection.Output };
            cmd.Parameters.AddRange(new[] { pBookingId, pPaymentRef, pTotalAmount });

            try
            {
                await cmd.ExecuteNonQueryAsync(ct);

                var bookingId   = Convert.ToInt32(pBookingId.Value ?? 0);
                var bookingCode = Convert.ToString(pPaymentRef.Value ?? "");
                var totalAmount = Convert.ToDecimal(pTotalAmount.Value ?? 0m);

                // QR flow giữ nguyên
                var qr = await _tingeeClient.CreateQrAsync(bookingId, totalAmount, bookingCode, ct);

                return new BookingResponseDto
                {
                    BookingId = bookingId,
                    TotalAmount = totalAmount,
                    PaymentQrUrl = qr.QrUrl,
                    PaymentQrImage = qr.QrCodeImage,
                    PaymentQrString = qr.QrCode,
                    BookingCode = bookingCode
                };
            }
            catch (MySqlException ex) when (ex.SqlState == "45000")
            {
                // Lỗi business SIGNAL từ proc (vd: Not enough seats)
                _logger.LogWarning(ex, "Booking failed (business): {@Dto}", dto);
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return false;
            if (booking.PaymentStatus == "paid") return true;

            booking.PaymentStatus = "paid";
            booking.PaymentTime = DateTime.UtcNow; // UTC cho chuẩn
            await _context.SaveChangesAsync();

            return true;
        }

        // Wrapper để tương thích code cũ (nếu nơi khác gọi CreateBookingAsync(dto) không truyền ct)
        public Task<BookingResponseDto> CreateBookingAsync(BookingDto dto)
            => CreateBookingAsync(dto, default);
    }
}
