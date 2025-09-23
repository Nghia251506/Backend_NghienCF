using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;
using Backend_Nghiencf.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Backend_Nghiencf.Helpers;

namespace Backend_Nghiencf.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public UserService(AppDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        // Tạo user mới
        public async Task<UserReadDto> CreateUserAsync(UserCreateDto dto)
        {
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PassWord = BCrypt.Net.BCrypt.HashPassword(dto.PassWord) // ✅ hash ngay từ đầu
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserReadDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
                // ❌ KHÔNG return PassWord để tránh lộ hash
            };
        }

        // Login
        public async Task<string?> LoginAsync(UserLoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == dto.UserName);
            if (user == null) return null;

            bool isValid = BCrypt.Net.BCrypt.Verify(dto.PassWord, user.PassWord);
            if (!isValid) return null;

            // return new UserReadDto
            // {
            //     Id = user.Id,
            //     UserName = user.UserName,
            //     Email = user.Email
            // };
            return _jwtHelper.GenerateToken(user);
        }

    }
}
