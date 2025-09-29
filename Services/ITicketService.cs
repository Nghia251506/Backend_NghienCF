using Backend_Nghiencf.Dtos;
using Backend_Nghiencf.Dtos.Common;
using Backend_Nghiencf.Dtos.Ticket;

namespace Backend_Nghiencf.Services;

public interface ITicketService
{
    Task<TicketReadDto> CreateAsync(int bookingId, DateTime? eventDate, CancellationToken ct = default);
    Task<TicketReadDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TicketReadDto>> GetByBookingAsync(int bookingId, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(int id, TicketStatusUpdateDto dto, CancellationToken ct = default);

    // NEW
    Task<PagedResult<TicketListItemDto>> GetAllAsync(TicketQuery query, CancellationToken ct = default);
}
