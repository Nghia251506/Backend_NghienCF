// Controllers/TingeeWebhookController.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        private static readonly TimeSpan AllowedSkew = TimeSpan.FromMinutes(5);

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
                // Cho phép đọc lại body nếu middleware khác đã đọc trước
                HttpContext.Request.EnableBuffering();

                var ts  = Request.Headers["x-request-timestamp"].ToString();
                var sig = Request.Headers["x-signature"].ToString();

                _logger.LogInformation("[WEBHOOK] >>> received at {Now:o}, ts={Ts}, sig-len={Len}, ip={IP}",
                    started, ts, sig?.Length, HttpContext.Connection.RemoteIpAddress?.ToString());

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

                // Verify signature: HMACSHA512(secret, $"{ts}:{body}")
                var expected = ComputeHmac512Hex(_opt.SecretToken, $"{ts}:{body}");
                if (!ConstantEquals(expected, sig))
                {
                    _logger.LogWarning("[WEBHOOK] signature mismatch. expected={Expected} got={Got}", expected, sig);
                    return Unauthorized();
                }
                _logger.LogInformation("[WEBHOOK] signature OK");

                // Verify timestamp (ts là GIỜ VIỆT NAM -> convert sang UTC)
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

                // Webhook biến động số dư: không có orderId/status
                var content = root.TryGetProperty("content", out var pContent) ? pContent.GetString() : null;
                var amount  = root.TryGetProperty("amount", out var pAmt) ? (long?)pAmt.GetInt64() : null;
                var acc     = root.TryGetProperty("accountNumber", out var pAcc) ? pAcc.GetString() : null;
                var vaAcc   = root.TryGetProperty("vaAccountNumber", out var pVa) ? pVa.GetString() : null;

                _logger.LogInformation("[WEBHOOK] parsed fields: acc={Acc}, va={Va}, amount={Amt}, content={Content}",
                    acc, vaAcc, amount, content);

                // Trích bookingId từ content: yêu cầu nội dung có 'PAY{digits}'
                if (!TryExtractBookingIdFromContent(content, out var bookingId))
                {
                    _logger.LogWarning("[WEBHOOK] cannot extract bookingId from content. content={Content}", content);
                    return Ok(new { code = "02", message = "Cannot resolve booking" });
                }

                // Kiểm tra số TK/VA nhận nếu có cấu hình
                var expectedAcc = _opt.Bank?.AccountNumber?.Trim();
                if (!string.IsNullOrWhiteSpace(expectedAcc))
                {
                    var matched = string.Equals(expectedAcc, acc, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(expectedAcc, vaAcc, StringComparison.OrdinalIgnoreCase);
                    if (!matched)
                    {
                        _logger.LogWarning("[WEBHOOK] account mismatch. expected={Exp} gotAcc={Acc} gotVa={Va}",
                            expectedAcc, acc, vaAcc);
                        return Ok(new { code = "03", message = "Account mismatch" });
                    }
                }

                // Tìm booking
                var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("[WEBHOOK] booking not found: {Id}", bookingId);
                    return Ok(new { code = "00", message = "OK (not found)" });
                }

                // Đối chiếu số tiền
                if (amount.HasValue)
                {
                    var expectedAmount = (long)Math.Round(booking.TotalAmount);
                    if (amount.Value != expectedAmount)
                    {
                        _logger.LogWarning("[WEBHOOK] amount mismatch for booking {Id}. expected={Exp} got={Got}",
                            bookingId, expectedAmount, amount.Value);
                        // Chính sách tuỳ bạn: ở đây trả OK nhưng không đổi trạng thái
                        return Ok(new { code = "04", message = "Amount mismatch" });
                    }
                }

                var old = (booking.PaymentStatus ?? "").ToLowerInvariant();
                var next = "paid";
                _logger.LogInformation("[WEBHOOK] booking {Id}: {Old} -> {Next}", booking.Id, old, next);

                if (old == next)
                {
                    _logger.LogInformation("[WEBHOOK] no state change");
                    return Ok(new { code = "00", message = "OK" });
                }

                // Phát hành vé idempotent
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

                // Cập nhật booking
                booking.PaymentStatus = next;
                booking.PaymentTime   = DateTime.UtcNow;

                var saved = await _db.SaveChangesAsync();
                _logger.LogInformation("[WEBHOOK] SaveChanges done: {Saved} changes", saved);

                var elapsed = DateTime.UtcNow - started;
                _logger.LogInformation("[WEBHOOK] <<< done in {Ms} ms", (int)elapsed.TotalMilliseconds);

                return Ok(new { code = "00", message = "OK" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WEBHOOK] exception");
                return StatusCode(500, new { code = "99", message = "Internal error" });
            }
        }

        // ===== Helpers =====

        private static bool TryExtractBookingIdFromContent(string? content, out long bookingId)
        {
            bookingId = 0;
            if (string.IsNullOrWhiteSpace(content)) return false;

            // Ưu tiên pattern PAY{digits} hoặc "PAY 12345" / "PAY#12345"
            var m = Regex.Match(content, @"PAY\s*#?\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && long.TryParse(m.Groups[1].Value, out bookingId)) return true;

            // Fallback (nếu bạn muốn): bắt dãy số >= 4 chữ số
            // var m2 = Regex.Match(content, @"\b(\d{4,})\b");
            // if (m2.Success && long.TryParse(m2.Groups[1].Value, out bookingId)) return true;

            return false;
        }

        private static bool TryParseTsVietnam(string ts, out DateTime utc)
        {
            // ts format: yyyyMMddHHmmssfff (GIỜ VN)
            if (DateTime.TryParseExact(ts, "yyyyMMddHHmmssfff",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var local))
            {
                // VN timezone
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
