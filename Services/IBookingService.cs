using Backend_Nghiencf.DTOs;

namespace Backend_Nghiencf.Services
{
    public interface IBookingService
    {
        Task<BookingResponseDto> CreateBookingAsync(BookingDto dto);
        Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId);
    }
}
