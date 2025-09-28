using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;

namespace Backend_Nghiencf.Services
{
    public interface IBookingService
    {
        Task<BookingResponseDto> CreateBookingAsync(BookingDto dto, CancellationToken ct = default);
        Task<IEnumerable<Booking>> GetAllSync();
        Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId);
        // 
    }

    public interface IBookingDevService
    {
        Task MarkPaidAndIssueTicketsAsync(int id, CancellationToken stoppingToken);
    }
}
