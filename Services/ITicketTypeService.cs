using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;
namespace Backend_Nghiencf.Services
{
    public interface ITicketTypeService
    {
        Task<List<TicketTypeReadDto>> GetByShowAsync(int showId, CancellationToken ct = default);
        Task<IEnumerable<TicketTypeReadDto>> GetAllAsync();
        Task<TicketTypeReadDto?> GetTypeById(int id);
        Task<TicketTypeReadDto?> CreateAsync(TicketTypeCreateDto dto);
        Task<bool> UpdateAsync(int id, TicketTypeUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}