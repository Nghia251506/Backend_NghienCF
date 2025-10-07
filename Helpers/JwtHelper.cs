using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Backend_Nghiencf.Models;

namespace Backend_Nghiencf.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;
        public JwtHelper(IConfiguration configuration) => _configuration = configuration;

        public string GenerateToken(User user)
        {
            var secretKey = _configuration["Jwt:SecretKey"] 
                            ?? throw new Exception("Jwt:SecretKey is missing");
            var issuer    = _configuration["Jwt:Issuer"];
            var audience  = _configuration["Jwt:Audience"];
            var expiresIn = int.TryParse(_configuration["Jwt:ExpiresInHours"], out var h) ? h : 1;

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role; // Chú ý hoa-thường

            var claims = new[]
            {
                // 👇 dùng claim chuẩn để ASP.NET nhận diện user id
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            
                // nếu bạn vẫn muốn giữ "sub" thì có thể thêm, nhưng NameIdentifier là chuẩn hơn
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            
                new Claim("username", user.UserName ?? ""),
                new Claim("email", user.Email ?? ""),
            
                // 👇 QUAN TRỌNG: role phải dùng ClaimTypes.Role để [Authorize(Roles="...")] hoạt động
                new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role),
            };

            var token = new JwtSecurityToken(
                issuer: issuer,            // nếu bạn không validate issuer/audience có thể để null
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiresIn),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
