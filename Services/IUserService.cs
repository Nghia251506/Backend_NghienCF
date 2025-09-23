using Backend_Nghiencf.DTOs;

namespace Backend_Nghiencf.Services
{
    public interface IUserService
    {
        Task<UserReadDto> CreateUserAsync(UserCreateDto dto);
        Task<string?> LoginAsync(UserLoginDto dto);
    }
}
