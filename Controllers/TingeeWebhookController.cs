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
        var ts  = Request.Headers["x-request-timestamp"].ToString();
        var sig = Request.Headers["x-signature"].ToString();
        if (string.IsNullOrWhiteSpace(ts) || string.IsNullOrWhiteSpace(sig)) return Unauthorized();

        using var sr = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await sr.ReadToEndAsync();

        var expected = ComputeHmac512Hex(_opt.SecretToken, $"{ts}:{body}");
        if (!ConstantEquals(expected, sig)) return Unauthorized();

        // chặn replay theo thời gian
        if (!TryParseTs(ts, out var reqUtc) || DateTime.UtcNow - reqUtc > AllowedSkew) return Unauthorized();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var orderIdStr = root.GetProperty("orderId").GetString();
        var statusRaw  = root.GetProperty("status").GetString(); // ví dụ SUCCESS/FAILED/PENDING

        if (!long.TryParse(orderIdStr, out var bookingId)) return Ok(new { code="02", message="Bad orderId" });

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return Ok(new { code="00", message="OK (not found)" });

        var old = (booking.PaymentStatus ?? "").ToLowerInvariant();
        var next = MapStatus(statusRaw);

        if (old == next) return Ok(new { code="00", message="OK" });

        // nếu chuyển sang failed từ pending => hoàn kho
        if (next == "failed" && old == "pending")
        {
            await _db.Database.ExecuteSqlRawAsync(@"
                UPDATE TicketTypes
                SET RemainingQuantity = RemainingQuantity + {0}
                WHERE Id = {1} AND ShowId = {2}
            ", booking.Quantity, booking.TicketTypeId, booking.ShowId);
        }

        booking.PaymentStatus = next;
        booking.PaymentTime   = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { code="00", message="OK" });
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
