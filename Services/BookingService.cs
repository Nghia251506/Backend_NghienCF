using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;

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
            _tingeeClient = tingeeClient;
            _logger = logger;
        }

        public async Task<IEnumerable<Booking>> GetAllSync()
        {
            return await _context.Bookings
                .AsNoTracking()
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
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// Tạo booking + gọi Tingee tạo giao dịch (orderId = bookingId) + trả QR của Tingee.
        /// </summary>
        public async Task<BookingResponseDto> CreateBookingAsync(BookingDto dto, CancellationToken ct = default)
        {
            if (dto.Quantity <= 0) throw new ArgumentException("Số lượng phải > 0");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);

                try
                {
                    // 1) Trừ kho atomically
                    var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE ticket_types
                        SET remaining_quantity = remaining_quantity - {dto.Quantity}
                        WHERE id = {dto.TicketTypeId} AND remaining_quantity >= {dto.Quantity};
                    ", ct);

                    if (affected == 0)
                    {
                        var remain = await _context.TicketTypes.AsNoTracking()
                            .Where(t => t.Id == dto.TicketTypeId)
                            .Select(t => (int?)t.RemainingQuantity)
                            .SingleOrDefaultAsync(ct) ?? 0;

                        throw new InvalidOperationException($"Không đủ số lượng vé (còn {remain}, yêu cầu {dto.Quantity}).");
                    }

                    // 2) Lấy thông tin loại vé
                    var type = await _context.TicketTypes.AsNoTracking()
                        .SingleAsync(t => t.Id == dto.TicketTypeId, ct);

                    var total = type.Price * dto.Quantity;

                    // 3) Tạo booking (pending)
                    var booking = new Booking
                    {
                        ShowId = type.ShowId,
                        TicketTypeId = dto.TicketTypeId,
                        CustomerName = dto.CustomerName?.Trim() ?? "",
                        Phone = dto.Phone?.Trim() ?? "",
                        Quantity = dto.Quantity,
                        TotalAmount = total,
                        PaymentStatus = "pending",
                        PaymentTime = null,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync(ct); // cần Id

                    _logger.LogInformation("[BOOKING] Created Id={Id} total={Total}", booking.Id, booking.TotalAmount);

                    // 4) GỌI TINGEE TẠO GIAO DỊCH (KHÔNG dùng generateQrPath)
                    //    BẮT BUỘC: orderId phải = booking.Id để webhook map được.
                    var createReq = new TingeeCreatePaymentRequest
                    {
                        OrderId = booking.Id.ToString(),
                        Amount = booking.TotalAmount,
                        Description = $"BOOK-{booking.Id}", // tuỳ bạn
                        // Các field khác theo tài liệu Tingee (nếu cần):
                        // AccountId = "...",
                        // Currency = "VND",
                        // WebhookProfileId = "...",
                        // ...
                    };

                    var tg = await _tingeeClient.CreatePaymentAsync(createReq, ct);

                    // 5) Lưu tham chiếu từ phía Tingee (nếu có) - tuỳ mô hình DB của bạn
                    // Nếu trong Booking có các trường này thì gán, nếu không thì bỏ qua.
                    try
                    {
                        booking.Provider = "tingee";
                        booking.ProviderOrderId = string.IsNullOrWhiteSpace(tg.OrderId) ? createReq.OrderId : tg.OrderId;
                        booking.ProviderTxnId   = tg.TransactionId;
                        await _context.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[BOOKING] Optional provider refs not saved (fields may not exist in model).");
                    }

                    // 6) Commit giao dịch DB
                    await tx.CommitAsync(ct);

                    // 7) Trả đúng QR Tingee về FE
                    return new BookingResponseDto
                    {
                        BookingId = booking.Id,
                        TotalAmount = booking.TotalAmount,
                        PaymentQrString = tg.QrString, // nếu SDK trả chuỗi 000201...
                        PaymentQrImage  = tg.QrImage,  // nếu SDK trả base64/image url
                        PaymentQrUrl    = tg.QrUrl     // nếu SDK trả link redirect/ảnh
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[BOOKING] Create failed. Rolling back & restore stock. typeId={TypeId} qty={Qty}",
                        dto.TicketTypeId, dto.Quantity);

                    // Rollback DB tx
                    await tx.RollbackAsync(ct);

                    // Cộng trả kho
                    await _context.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE ticket_types
                        SET remaining_quantity = remaining_quantity + {dto.Quantity}
                        WHERE id = {dto.TicketTypeId};
                    ", ct);

                    throw;
                }
            });
        }

        /// <summary>
        /// (Tuỳ chọn) Xác nhận thanh toán thủ công/dev. Webhook mới là chuẩn.
        /// </summary>
        public async Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.PaymentStatus = "paid";
            booking.PaymentTime = DateTime.UtcNow;

            // (gợi ý) idempotent phát hành vé nếu cần, hoặc để webhook xử lý
            await _context.SaveChangesAsync();
            return true;
        }
    }

    // ==== Gợi ý contract ITingeeClient & models (điều chỉnh theo SDK thực tế) ====

    public interface ITingeeClient
    {
        Task<TingeeCreatePaymentResult> CreatePaymentAsync(TingeeCreatePaymentRequest req, CancellationToken ct = default);
    }

    public sealed class TingeeCreatePaymentRequest
    {
        public string OrderId { get; set; } = default!;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        // public string? AccountId { get; set; }
        // public string Currency { get; set; } = "VND";
        // public string? WebhookProfileId { get; set; }
        // ... các field khác theo tài liệu Tingee
    }

    public sealed class TingeeCreatePaymentResult
    {
        public string? OrderId { get; set; }
        public string? TransactionId { get; set; }
        public string? QrString { get; set; } // "000201..."
        public string? QrImage { get; set; }  // data:image/... hoặc url ảnh
        public string? QrUrl { get; set; }    // link redirect/ảnh
    }
}
