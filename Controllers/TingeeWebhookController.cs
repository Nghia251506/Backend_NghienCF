// Controllers/TingeeWebhookController.cs
// Controllers/TingeeWebhookController.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.Models;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.Models;
using Backend_Nghiencf.Options;

namespace Backend_Nghiencf.Controllers
{
    [ApiController]
    [Route("api/tingee/webhook")]
    public sealed class TingeeWebhookController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TingeeWebhookController> _logger;
        private readonly TingeeOptions _opt;

        // Cho phép chênh lệch ±5 phút
        private static readonly TimeSpan AllowedSkew = TimeSpan.FromMinutes(5);
        // Cho phép lệch số tiền (nếu cần) — ví dụ 1.000đ
        private const long AmountTolerance = 0;
namespace Backend_Nghiencf.Controllers
{
    [ApiController]
    [Route("api/tingee/webhook")]
    public sealed class TingeeWebhookController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TingeeWebhookController> _logger;
        private readonly TingeeOptions _opt;

        // Cho phép chênh lệch ±5 phút
        private static readonly TimeSpan AllowedSkew = TimeSpan.FromMinutes(5);
        // Cho phép lệch số tiền (nếu cần) — ví dụ 1.000đ
        private const long AmountTolerance = 0;

        public TingeeWebhookController(
            AppDbContext db,
            ILogger<TingeeWebhookController> logger,
            IOptions<TingeeOptions> opt)
        {
            _db = db;
            _logger = logger;
            _opt = opt.Value;
        }
        public TingeeWebhookController(
            AppDbContext db,
            ILogger<TingeeWebhookController> logger,
            IOptions<TingeeOptions> opt)
        {
            _db = db;
            _logger = logger;
            _opt = opt.Value;
        }

        [HttpPost]
        public async Task<IActionResult> HandleAsync()
        {
            var started = DateTime.UtcNow;

            try
            {
                // Cho phép đọc lại body nếu có middleware đọc trước
                HttpContext.Request.EnableBuffering();
        [HttpPost]
        public async Task<IActionResult> HandleAsync()
        {
            var started = DateTime.UtcNow;

            try
            {
                // Cho phép đọc lại body nếu có middleware đọc trước
                HttpContext.Request.EnableBuffering();

                var ts  = Request.Headers["x-request-timestamp"].ToString();
                var sig = Request.Headers["x-signature"].ToString();
                var ts  = Request.Headers["x-request-timestamp"].ToString();
                var sig = Request.Headers["x-signature"].ToString();

                _logger.LogInformation("[WEBHOOK] >>> received at {Now:o}, ts={Ts}, sig-len={Len}, ip={IP}",
                    started, ts, sig?.Length, HttpContext.Connection.RemoteIpAddress?.ToString());
                _logger.LogInformation("[WEBHOOK] >>> received at {Now:o}, ts={Ts}, sig-len={Len}, ip={IP}",
                    started, ts, sig?.Length, HttpContext.Connection.RemoteIpAddress?.ToString());

                if (string.IsNullOrWhiteSpace(ts) || string.IsNullOrWhiteSpace(sig))
                {
                    _logger.LogWarning("[WEBHOOK] missing required headers");
                    return Unauthorized();
                }
                if (string.IsNullOrWhiteSpace(ts) || string.IsNullOrWhiteSpace(sig))
                {
                    _logger.LogWarning("[WEBHOOK] missing required headers");
                    return Unauthorized();
                }

                // Đọc body raw
                string body;
                using (var sr = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                {
                    body = await sr.ReadToEndAsync();
                    Request.Body.Position = 0;
                }
                _logger.LogInformation("[WEBHOOK] raw body: {Body}", body);
                // Đọc body raw
                string body;
                using (var sr = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                {
                    body = await sr.ReadToEndAsync();
                    Request.Body.Position = 0;
                }
                _logger.LogInformation("[WEBHOOK] raw body: {Body}", body);

                // Verify signature: HMACSHA512(secret, $"{ts}:{body}")
                var expected = ComputeHmac512Hex(_opt.SecretToken, $"{ts}:{body}");
                if (!ConstantEquals(expected, sig))
                {
                    _logger.LogWarning("[WEBHOOK] signature mismatch. expected={Expected} got={Got}", expected, sig);
                    return Unauthorized();
                }
                _logger.LogInformation("[WEBHOOK] signature OK");
                // Verify signature: HMACSHA512(secret, $"{ts}:{body}")
                var expected = ComputeHmac512Hex(_opt.SecretToken, $"{ts}:{body}");
                if (!ConstantEquals(expected, sig))
                {
                    _logger.LogWarning("[WEBHOOK] signature mismatch. expected={Expected} got={Got}", expected, sig);
                    return Unauthorized();
                }
                _logger.LogInformation("[WEBHOOK] signature OK");

                // Parse timestamp (ts là GIỜ VIỆT NAM → convert sang UTC)
                if (!TryParseTsVietnam(ts, out var reqUtc))
                {
                    _logger.LogWarning("[WEBHOOK] bad timestamp format: {Ts}", ts);
                    return Unauthorized();
                }
                var skew = DateTime.UtcNow - reqUtc;
                if (skew > AllowedSkew || skew < -AllowedSkew)
                {
                    _logger.LogWarning("[WEBHOOK] timestamp skew too large: {Skew}", skew);
                    return Unauthorized();
                }
                _logger.LogInformation("[WEBHOOK] timestamp OK (skew={Skew})", skew);
                // Parse timestamp (ts là GIỜ VIỆT NAM → convert sang UTC)
                if (!TryParseTsVietnam(ts, out var reqUtc))
                {
                    _logger.LogWarning("[WEBHOOK] bad timestamp format: {Ts}", ts);
                    return Unauthorized();
                }
                var skew = DateTime.UtcNow - reqUtc;
                if (skew > AllowedSkew || skew < -AllowedSkew)
                {
                    _logger.LogWarning("[WEBHOOK] timestamp skew too large: {Skew}", skew);
                    return Unauthorized();
                }
                _logger.LogInformation("[WEBHOOK] timestamp OK (skew={Skew})", skew);

                // Parse JSON
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                // Parse JSON
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // Một số webhook (biến động số dư) không có orderId/status mà chỉ có content/amount/… 
                var content = GetString(root, "content");
                var statusRaw = GetString(root, "status");           // nếu là webhook đơn hàng thì có thể có
                var orderIdStr = GetString(root, "orderId") 
                              ?? GetString(root, "orderCode");        // tuỳ môi trường
                var bank = GetString(root, "bank");
                var acc  = GetString(root, "accountNumber");
                var va   = GetString(root, "vaAccountNumber");
                var amount = GetLong(root, "amount");

                _logger.LogInformation("[WEBHOOK] fields: orderId={OrderId}, status={Status}, bank={Bank}, acc={Acc}, va={Va}, amount={Amt}, content={Content}",
                    orderIdStr, statusRaw, bank, acc, va, amount, content);
                // Một số webhook (biến động số dư) không có orderId/status mà chỉ có content/amount/… 
                var content = GetString(root, "content");
                var statusRaw = GetString(root, "status");           // nếu là webhook đơn hàng thì có thể có
                var orderIdStr = GetString(root, "orderId") 
                              ?? GetString(root, "orderCode");        // tuỳ môi trường
                var bank = GetString(root, "bank");
                var acc  = GetString(root, "accountNumber");
                var va   = GetString(root, "vaAccountNumber");
                var amount = GetLong(root, "amount");

                _logger.LogInformation("[WEBHOOK] fields: orderId={OrderId}, status={Status}, bank={Bank}, acc={Acc}, va={Va}, amount={Amt}, content={Content}",
                    orderIdStr, statusRaw, bank, acc, va, amount, content);

                // Resolve bookingId
                long bookingId;
                if (!long.TryParse(orderIdStr, out bookingId))
                {
                    if (!TryExtractRefFromContent(content, out var paymentRef, out bookingId))
                    {
                        _logger.LogWarning("[WEBHOOK] cannot resolve bookingId. orderId={OrderId}, content={Content}", orderIdStr, content);
                        return Ok(new { code = "02", message = "Cannot resolve booking" });
                    }
                    _logger.LogInformation("[WEBHOOK] bookingId extracted from content: {Id}", bookingId);

                    // (Khuyến nghị) nếu bạn có cột Booking.PaymentRef (PAY{Id} hoặc BOOKING{Id})
                    // thì có thể ưu tiên lookup theo PaymentRef trước:
                    if (!string.IsNullOrWhiteSpace(paymentRef))
                    {
                        var bByRef = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingCode == paymentRef);
                        if (bByRef != null) bookingId = bByRef.Id;
                    }
                }

                // (Tuỳ chọn) Check acc/vaAcc theo config
                var expectedAcc = _opt.Bank?.AccountNumber?.Trim();
                if (!string.IsNullOrWhiteSpace(expectedAcc))
                {
                    var matched = string.Equals(expectedAcc, acc, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(expectedAcc, va, StringComparison.OrdinalIgnoreCase);
                    if (!matched)
                    {
                        _logger.LogWarning("[WEBHOOK] account mismatch. expected={Exp} gotAcc={Acc} gotVa={Va}",
                            expectedAcc, acc, va);
                        return Ok(new { code = "03", message = "Account mismatch" });
                    }
                }

                // Lấy booking
                var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("[WEBHOOK] booking not found: {Id}", bookingId);
                    return Ok(new { code = "00", message = "OK (not found)" });
                }

                // Đối chiếu số tiền (nếu payload có amount)
                if (amount.HasValue)
                {
                    var expectedAmount = (long)Math.Round(booking.TotalAmount);
                    if (Math.Abs(amount.Value - expectedAmount) > AmountTolerance)
                    {
                        _logger.LogWarning("[WEBHOOK] amount mismatch for booking {Id}. expected={Exp} got={Got}",
                            bookingId, expectedAmount, amount.Value);
                        // Chính sách tuỳ chọn: ở đây không đổi trạng thái
                        return Ok(new { code = "04", message = "Amount mismatch" });
                    }
                }

                // Quyết định trạng thái
                var next = MapStatus(statusRaw); // nếu không có status -> 'paid' khi đã match được giao dịch
                if (next == "pending")
                {
                    // Webhook biến động số dư (không có status): coi như đã thanh toán
                    next = "paid";
                }

                var old = (booking.PaymentStatus ?? "").ToLowerInvariant();
                _logger.LogInformation("[WEBHOOK] booking {Id}: {Old} -> {Next}", booking.Id, old, next);
                var old = (booking.PaymentStatus ?? "").ToLowerInvariant();
                _logger.LogInformation("[WEBHOOK] booking {Id}: {Old} -> {Next}", booking.Id, old, next);

                if (old == next)
                {
                    _logger.LogInformation("[WEBHOOK] no state change");
                    return Ok(new { code = "00", message = "OK" });
                }
                if (old == next)
                {
                    _logger.LogInformation("[WEBHOOK] no state change");
                    return Ok(new { code = "00", message = "OK" });
                }

                // Nếu failed từ pending → hoàn kho
                if (next == "failed" && old == "pending")
                {
                    _logger.LogInformation("[WEBHOOK] restore stock +{Qty} for type={Type} show={Show}",
                        booking.Quantity, booking.TicketTypeId, booking.ShowId);
                // Nếu failed từ pending → hoàn kho
                if (next == "failed" && old == "pending")
                {
                    _logger.LogInformation("[WEBHOOK] restore stock +{Qty} for type={Type} show={Show}",
                        booking.Quantity, booking.TicketTypeId, booking.ShowId);

                    await _db.Database.ExecuteSqlRawAsync(@"
                        UPDATE TicketTypes
                        SET RemainingQuantity = RemainingQuantity + {0}
                        WHERE Id = {1} AND ShowId = {2}",
                        booking.Quantity, booking.TicketTypeId, booking.ShowId);
                }
                    await _db.Database.ExecuteSqlRawAsync(@"
                        UPDATE TicketTypes
                        SET RemainingQuantity = RemainingQuantity + {0}
                        WHERE Id = {1} AND ShowId = {2}",
                        booking.Quantity, booking.TicketTypeId, booking.ShowId);
                }

                // Nếu paid → phát hành vé (idempotent)
                if (next == "paid")
                {
                    var hasTickets = await _db.Tickets.AnyAsync(t => t.BookingId == booking.Id);
                    if (!hasTickets)
                    {
                        var tickets = Enumerable.Range(0, booking.Quantity).Select(_ => new Ticket
                        {
                            BookingId  = booking.Id,
                            TicketCode = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
                            Status     = "active",
                            IssuedAt   = DateTime.UtcNow
                        }).ToList();
                // Nếu paid → phát hành vé (idempotent)
                if (next == "paid")
                {
                    var hasTickets = await _db.Tickets.AnyAsync(t => t.BookingId == booking.Id);
                    if (!hasTickets)
                    {
                        var tickets = Enumerable.Range(0, booking.Quantity).Select(_ => new Ticket
                        {
                            BookingId  = booking.Id,
                            TicketCode = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
                            Status     = "active",
                            IssuedAt   = DateTime.UtcNow
                        }).ToList();

                        _db.Tickets.AddRange(tickets);
                        _logger.LogInformation("[WEBHOOK] issued {Count} ticket(s) for booking {Id}", tickets.Count, booking.Id);
                    }
                    else
                    {
                        _logger.LogInformation("[WEBHOOK] tickets already issued for booking {Id}", booking.Id);
                    }
                }
                        _db.Tickets.AddRange(tickets);
                        _logger.LogInformation("[WEBHOOK] issued {Count} ticket(s) for booking {Id}", tickets.Count, booking.Id);
                    }
                    else
                    {
                        _logger.LogInformation("[WEBHOOK] tickets already issued for booking {Id}", booking.Id);
                    }
                }

                // Cập nhật booking
                booking.PaymentStatus = next;
                booking.PaymentTime   = DateTime.UtcNow;
                // Cập nhật booking
                booking.PaymentStatus = next;
                booking.PaymentTime   = DateTime.UtcNow;

                var saved = await _db.SaveChangesAsync();
                _logger.LogInformation("[WEBHOOK] SaveChanges done: {Saved} changes", saved);
                var saved = await _db.SaveChangesAsync();
                _logger.LogInformation("[WEBHOOK] SaveChanges done: {Saved} changes", saved);

                var elapsed = DateTime.UtcNow - started;
                _logger.LogInformation("[WEBHOOK] <<< done in {Ms} ms", (int)elapsed.TotalMilliseconds);
                var elapsed = DateTime.UtcNow - started;
                _logger.LogInformation("[WEBHOOK] <<< done in {Ms} ms", (int)elapsed.TotalMilliseconds);

        return Ok(new { code="00", message="OK" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[WEBHOOK] exception");
        return StatusCode(500, new { code="99", message="Internal error" });
    }
}

        // ===== Helpers =====

        private static string? GetString(JsonElement root, string name)
            => root.TryGetProperty(name, out var p) ? p.GetString() : null;

        private static long? GetLong(JsonElement root, string name)
            => root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number
                ? p.GetInt64()
                : (long?)null;

        private static bool TryExtractRefFromContent(string? content, out string? paymentRef, out long bookingId)
        {
            paymentRef = null;
            bookingId = 0;
            if (string.IsNullOrWhiteSpace(content)) return false;

            // Bắt: PAY123, PAY-123, PAY#123, BOOKING123, BOOKING 123, v.v.
            // (?i) = ignore case
            var m = Regex.Match(content, @"(?i)\b(?:pay|booking)\s*[#:\-]?\s*(\d{1,12})\b");
            if (!m.Success) return false;

            var digits = m.Groups[1].Value;

            // CHỌN 1 CHUẨN và dùng XUYÊN SUỐT:
            // - Nếu DB bạn lưu PaymentRef là "PAY{Id}" thì giữ nguyên PAY:
            // paymentRef = "PAY" + digits;

            // - Nếu bạn muốn là "BOOKING{Id}" thì nhất quán toàn hệ thống với format đó:
            paymentRef = "BOOKING" + digits;

            long.TryParse(digits, out bookingId);
            return true;
        }

        private static string MapStatus(string? s)
        {
            var x = (s ?? "").Trim().ToUpperInvariant();
            return x switch
            {
                "SUCCESS" or "PAID" or "COMPLETED" => "paid",
                "FAILED" or "CANCELLED" or "ERROR" => "failed",
                _ => "pending"
            };
        }

        private static bool TryParseTsVietnam(string ts, out DateTime utc)
        {
            // ts: yyyyMMddHHmmssfff (GIỜ VIỆT NAM)
            if (DateTime.TryParseExact(ts, "yyyyMMddHHmmssfff",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var local))
            {
                var tzId = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                    ? "SE Asia Standard Time"
                    : "Asia/Ho_Chi_Minh";
                var vn = TimeZoneInfo.FindSystemTimeZoneById(tzId);

                var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
                utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, vn);
                return true;
            }
            utc = default;
            return false;
        }

        private static string ComputeHmac512Hex(string secret, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret ?? ""));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static bool ConstantEquals(string a, string b)
        {
            var ba = Encoding.UTF8.GetBytes(a ?? "");
            var bb = Encoding.UTF8.GetBytes(b ?? "");
            if (ba.Length != bb.Length) return false;
            var diff = 0;
            for (int i = 0; i < ba.Length; i++) diff |= ba[i] ^ bb[i];
            return diff == 0;
        }
    }
}
