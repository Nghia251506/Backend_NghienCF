using Backend_Nghiencf.DTOs;
namespace Backend_Nghiencf.Models
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public UserReadDto User { get; set; }
    }

}