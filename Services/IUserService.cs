using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;
namespace Backend_Nghiencf.Services
{
    public interface IUserService
    {
        Task<UserReadDto> CreateUserAsync(UserCreateDto dto);
        Task<AuthResponse?> LoginAsync(UserLoginDto dto);
    }
}
