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

            // Lấy connection gốc từ DbContext (Pomelo => MySqlConnection)
            await using var conn = (MySqlConnection)_context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            await using var cmd = new MySqlCommand("sp_create_booking", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // ===== INPUT params (khớp đúng tên trong PROC) =====
            cmd.Parameters.AddWithValue("@p_show_id", dto.ShowId);
            cmd.Parameters.AddWithValue("@p_ticket_type_id", dto.TicketTypeId);
            cmd.Parameters.AddWithValue("@p_customer_name", dto.CustomerName?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@p_phone", dto.Phone?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@p_quantity", dto.Quantity);

            // ===== OUTPUT params (khớp đúng tên/kiểu trong PROC) =====
            var pBookingId = new MySqlParameter("@p_booking_id", MySqlDbType.Int64) { Direction = ParameterDirection.Output };
            var pPaymentRef = new MySqlParameter("@p_payment_ref", MySqlDbType.VarChar, 64) { Direction = ParameterDirection.Output };
            var pTotalAmount = new MySqlParameter("@p_total_amount", MySqlDbType.Decimal) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(pBookingId);
            cmd.Parameters.Add(pPaymentRef);
            cmd.Parameters.Add(pTotalAmount);

            try
            {
                // Proc đã: trừ kho atomically + insert booking (pending) + set PaymentRef
                await cmd.ExecuteNonQueryAsync(ct);

                var bookingId = Convert.ToInt32(pBookingId.Value ?? 0);
                var totalAmount = Convert.ToDecimal(pTotalAmount.Value ?? 0m);

                // Giữ nguyên luồng QR của bạn
                var qr = await _tingeeClient.CreateQrAsync(bookingId, totalAmount, ct);

                return new BookingResponseDto
                {
                    BookingId = bookingId,
                    TotalAmount = totalAmount,
                    PaymentQrUrl = qr.QrUrl,
                    PaymentQrImage = qr.QrCodeImage,
                    PaymentQrString = qr.QrCode
                };
            }
            catch (MySqlException ex) when (ex.SqlState == "45000")
            {
                // Lỗi business SIGNAL từ proc (vd hết kho)
                throw new InvalidOperationException(ex.Message);
            }
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
