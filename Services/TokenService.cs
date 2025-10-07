// Services/TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "ckk";
    public string Audience { get; init; } = "ckk-web";
    public string Secret { get; init; } = "super-long-256-bit-secret-change-me";
    public int AccessMinutes { get; init; } = 15;
    public int RefreshDays { get; init; } = 30;
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _opt;
    private readonly byte[] _key;

    public TokenService(IOptions<JwtOptions> opt)
    {
        _opt = opt.Value;
        _key = Encoding.UTF8.GetBytes(_opt.Secret);
    }

    public string CreateAccessToken(int userId, string userName, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, userName),
            // new Claim(ClaimTypes.Role, role ?? "admin")s
        };

        var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public (string token, DateTime expires, string hashed) CreateRefreshToken()
    {
        // random 256-bit
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes);
        var expires = DateTime.UtcNow.AddDays(_opt.RefreshDays);
        // lưu bản hash vào DB để có thể thu hồi (không lưu thẳng plain token)
        var hashed = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        return (token, expires, hashed);
    }
}
