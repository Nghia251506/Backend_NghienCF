using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Backend_Nghiencf.Options;
using Backend_Nghiencf.Data;     // DbContext của bạn
using Backend_Nghiencf.Models;   // Booking entity

[ApiController]
[Route("api/tingee/webhook")]
public sealed class TingeeWebhookController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<TingeeWebhookController> _logger;
    private readonly TingeeOptions _opt;
    private static readonly TimeSpan AllowedSkew = TimeSpan.FromMinutes(5);

    public TingeeWebhookController(AppDbContext db, ILogger<TingeeWebhookController> logger, IOptions<TingeeOptions> opt)
    {
        _db = db; _logger = logger; _opt = opt.Value;
    }

    [HttpPost]
public async Task<IActionResult> HandleAsync()
{
    var started = DateTime.UtcNow;
    try
    {
        // 0) Cho phép đọc body nhiều lần (nếu có middleware đọc trước)
        HttpContext.Request.EnableBuffering();

        var ts  = Request.Headers["x-request-timestamp"].ToString();
        var sig = Request.Headers["x-signature"].ToString();

        _logger.LogInformation("[WEBHOOK] >>> received at {Now:o}, ts={Ts}, sig-len={Len}, ip={IP}",
            started, ts, sig?.Length, HttpContext.Connection.RemoteIpAddress?.ToString());

        if (string.IsNullOrWhiteSpace(ts) || string.IsNullOrWhiteSpace(sig))
        {
            _logger.LogWarning("[WEBHOOK] missing headers");
            return Unauthorized();
        }

        // 1) đọc body raw
        string body;
        using (var sr = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            body = await sr.ReadToEndAsync();
            Request.Body.Position = 0; // reset để có thể đọc lại nếu cần
        }
        _logger.LogInformation("[WEBHOOK] raw body: {Body}", body);

        // 2) verify signature
        var expected = ComputeHmac512Hex(_opt.SecretToken, $"{ts}:{body}");
        if (!ConstantEquals(expected, sig))
        {
            _logger.LogWarning("[WEBHOOK] signature mismatch. expected={Expected} got={Got}", expected, sig);
            return Unauthorized();
        }
        _logger.LogInformation("[WEBHOOK] signature OK");

        // 3) verify timestamp (tạm nới khi test)
        if (!TryParseTs(ts, out var reqUtc))
        {
            _logger.LogWarning("[WEBHOOK] bad timestamp format: {Ts}", ts);
            return Unauthorized();
        }
        var skew = DateTime.UtcNow - reqUtc;
        if (skew > AllowedSkew)
        {
            _logger.LogWarning("[WEBHOOK] too old. now={Now:o} req={Req:o} skew={Skew}", DateTime.UtcNow, reqUtc, skew);
            return Unauthorized();
        }
        _logger.LogInformation("[WEBHOOK] timestamp OK (skew={Skew})", skew);

        // 4) parse JSON, lấy orderId/status
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var orderIdStr = root.TryGetProperty("orderId", out var pOrder) ? pOrder.GetString() : null;
        var statusRaw  = root.TryGetProperty("status", out var pStatus) ? pStatus.GetString() : null;

        _logger.LogInformation("[WEBHOOK] payload: orderId={OrderId}, status={Status}", orderIdStr, statusRaw);

        if (!long.TryParse(orderIdStr, out var bookingId))
        {
            _logger.LogWarning("[WEBHOOK] bad orderId (not a long): {OrderId}", orderIdStr);
            return Ok(new { code="02", message="Bad orderId" });
        }

        // 5) tìm booking
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null)
        {
            _logger.LogWarning("[WEBHOOK] booking not found: {Id}", bookingId);
            return Ok(new { code="00", message="OK (not found)" });
        }

        var old = (booking.PaymentStatus ?? "").ToLowerInvariant();
        var next = MapStatus(statusRaw);
        _logger.LogInformation("[WEBHOOK] booking {Id}: {Old} -> {Next}", booking.Id, old, next);

        if (old == next)
        {
            _logger.LogInformation("[WEBHOOK] no state change");
            return Ok(new { code="00", message="OK" });
        }

        // 6) nếu FAILED từ PENDING => hoàn kho
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

        // 7) nếu PAID => phát hành vé (idempotent)
        if (next == "paid")
        {
            // tránh phát hành trùng nếu đã có vé
            var hasTickets = await _db.Tickets.AnyAsync(t => t.BookingId == booking.Id);
            if (!hasTickets)
            {
                var tickets = Enumerable.Range(0, booking.Quantity).Select(i => new Ticket
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
                _logger.LogInformation("[WEBHOOK] tickets already issued for booking {Id} (idempotent)", booking.Id);
            }
        }

        // 8) update booking
        booking.PaymentStatus = next;
        booking.PaymentTime   = DateTime.UtcNow;

        var saved = await _db.SaveChangesAsync();
        _logger.LogInformation("[WEBHOOK] SaveChanges done: {Saved} changes", saved);

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
        for (int i=0;i<ba.Length;i++) diff |= ba[i]^bb[i];
        return diff == 0;
    }

    private static bool TryParseTs(string ts, out DateTime utc)
    {
        // Tingee thường yyyyMMddHHmmssfff UTC
        if (DateTime.TryParseExact(ts, "yyyyMMddHHmmssfff",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var dt))
        { utc = dt; return true; }
        utc = default; return false;
    }
}
