using Backend_Nghiencf.DTOs;

namespace Backend_Nghiencf.Services
{
    public interface IShowService
    {
        Task<IEnumerable<ShowReadDto>> GetAllAsync();
        Task<ShowReadDto?> GetShowByTitleAsync(string Title);
        Task<ShowReadDto?> CreateAsync(ShowCreateDto dto);
        Task<ShowReadDto?> UpdateAsync(string Title, ShowUpdateDto dto);
        Task<bool> DeleteAsync(int Id);
    }
}