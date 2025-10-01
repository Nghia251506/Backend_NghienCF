// Services/IBookingDevService.cs
namespace Backend_Nghiencf.Services;
public interface IBookingDevService
{
    Task<int> MarkPaidAndIssueTicketsAsync(int bookingId, CancellationToken ct = default);
}
