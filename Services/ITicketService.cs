using Backend_Nghiencf.Dtos.Ticket;

namespace Backend_Nghiencf.Services;

public interface ITicketService
{
    Task<TicketReadDto> CreateAsync(int bookingId, DateTime? eventDate, CancellationToken ct = default);
    Task<TicketReadDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TicketReadDto>> GetByBookingAsync(int bookingId, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(int id, string status, CancellationToken ct = default);
}
