// Options/TingeeOptions.cs
namespace Backend_Nghiencf.Options;

public sealed class TingeeOptions
{
    public string Environment { get; init; } = "Live"; // "Sandbox" | "Live"
    public string SandboxBaseUrl { get; init; }
    public string LiveBaseUrl { get; init; } 
    public string ClientId { get; init; }     // x-client-id
    public string SecretToken { get; init; }   // HMAC key
    public string GenerateVietQrPath { get; init; }
    public BankOptions Bank { get; init; } = new(); // 👈 tránh null

    public sealed class BankOptions
    {
        public string? BankName { get; init; }
        public string? AccountNumber { get; init; }
        public string? AccountName { get; init; }
    }
}
