using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Backend_Nghiencf.Options;
using Microsoft.Extensions.Options;

public sealed class TingeeClient : ITingeeClient
{
    private readonly HttpClient _http;
    private readonly TingeeOptions _opt;
    private readonly ILogger<TingeeClient> _log;

    public TingeeClient(HttpClient http, IOptions<TingeeOptions> opt, ILogger<TingeeClient> log)
    {
        _http = http;
        _opt = opt.Value;
        _log = log;

        var baseUrl = string.Equals(_opt.Environment, "Live", StringComparison.OrdinalIgnoreCase)
            ? _opt.LiveBaseUrl
            : _opt.SandboxBaseUrl;

        if (string.IsNullOrWhiteSpace(_opt.ClientId))
            throw new InvalidOperationException("Tingee ClientId is not configured.");
        if (string.IsNullOrWhiteSpace(_opt.SecretToken))
            throw new InvalidOperationException("Tingee SecretToken is not configured.");

        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        _http.BaseAddress = new Uri(baseUrl);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _log.LogInformation("Tingee base={Base} env={Env} clientId={Client}",
            _http.BaseAddress, _opt.Environment, _opt.ClientId);
    }

    private static TimeZoneInfo VietnamTz()
    {
        try
        {
            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            // Linux/macOS
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch
        {
            // fallback UTC+7
            return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
        }
    }

    public async Task<string> CreateQrAsync(long bookingId, decimal amount, CancellationToken ct = default)
    {
        // ==== validate Bank config (theo yêu cầu Tingee) ====
        var bankName = _opt.Bank?.BankName?.Trim().ToUpperInvariant();
        var accountNumber = _opt.Bank?.AccountNumber?.Trim();
        var accountName = _opt.Bank?.AccountName?.Trim();

        if (string.IsNullOrWhiteSpace(bankName))
            throw new InvalidOperationException("Tingee:Bank:BankName is required.");
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new InvalidOperationException("Tingee:Bank:AccountNumber is required.");

        // ==== build body JSON CHÍNH XÁC để ký & gửi ====
        var payload = new
        {
            orderCode   = bookingId.ToString(),
            amount      = (long)Math.Round(amount),
            description = $"Booking #{bookingId}",
            currency    = "VND",
            // theo doc/lỗi trả về
            bankName    = bankName,          // VCB/BIDV/...
            accountNumber = accountNumber,   // số TK nhận
            accountName = accountName        // nếu doc yêu cầu, không có cũng không sao
        };

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };
        var bodyJson = JsonSerializer.Serialize(payload, jsonOptions);

        // ==== timestamp VN (UTC+7) dạng yyyyMMddHHmmssSSS ====
        var vnTz = VietnamTz();
        var ts = TimeZoneInfo.ConvertTime(DateTime.UtcNow, vnTz)
                             .ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);

        // ==== signature: HMACSHA512(secret, $"{ts}:{bodyJson}") ====
        var toSign = $"{ts}:{bodyJson}";
        var signature = ComputeHmacSha512Hex(_opt.SecretToken, toSign);

        var path = _opt.GenerateVietQrPath.StartsWith("/") ? _opt.GenerateVietQrPath : "/" + _opt.GenerateVietQrPath;

        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation("x-client-id", _opt.ClientId);
        req.Headers.TryAddWithoutValidation("x-request-timestamp", ts);
        req.Headers.TryAddWithoutValidation("x-signature", signature);

        var res = await _http.SendAsync(req, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Tingee error {(int)res.StatusCode}: {raw}");

        // ==== parse kết quả linh hoạt (qrUrl | data.qrUrl | data.qrCodeImage) ====
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        // 1) top-level `qrUrl`
        if (root.TryGetProperty("qrUrl", out var topQrUrl) && topQrUrl.ValueKind == JsonValueKind.String)
            return topQrUrl.GetString()!;

        // 2) `data.qrUrl` hoặc `data.qrCodeImage`
        if (root.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("qrUrl", out var qr2) && qr2.ValueKind == JsonValueKind.String)
                return qr2.GetString()!;

            if (data.TryGetProperty("qrCodeImage", out var qrImg) && qrImg.ValueKind == JsonValueKind.String)
                return qrImg.GetString()!; // data URL PNG -> dùng trực tiếp cho <img src=...>
        }

        throw new InvalidOperationException("Không tìm thấy 'qrUrl' hoặc 'qrCodeImage' trong phản hồi Tingee: " + raw);
    }

    private static string ComputeHmacSha512Hex(string secret, string message)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).ToLowerInvariant();
    }
}


public interface ITingeeClient
{
    Task<string> CreateQrAsync(long bookingId, decimal amount, CancellationToken ct = default);
}