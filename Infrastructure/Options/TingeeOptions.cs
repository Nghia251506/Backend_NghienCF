// Options/TingeeOptions.cs
namespace Backend_Nghiencf.Options;

public sealed class TingeeOptions
{
    public string Environment { get; init; } = "Sandbox"; // "Sandbox" | "Live"
    public string SandboxBaseUrl { get; init; } = "https://uat-open-api.tingee.vn";
    public string LiveBaseUrl { get; init; } = "https://open-api.tingee.vn";
    public string ClientId { get; init; } = "c935cbc4a520d73f5fb61e45041eaa3f";     // x-client-id
    public string SecretToken { get; init; } = "ndQAKxam2hcn5Pvf6HYgkOFGRHwDvF8gKpVvwX/syW8=";  // HMAC key
    public string GenerateVietQrPath { get; init; } = "/api/v1/payments/qr";
    public BankOptions Bank { get; init; } = new(); // 👈 tránh null

    public sealed class BankOptions
    {
        public string? BankName { get; init; }
        public string? AccountNumber { get; init; }
        public string? AccountName { get; init; }
    }
}
