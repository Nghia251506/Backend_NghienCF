// Dtos/Ticket/TicketCreateDto.cs
namespace Backend_Nghiencf.Dtos.Ticket;

public sealed class TicketCreateDto
{
    public int BookingId { get; set; }
    /// <summary>
    /// Ngày sự kiện (nếu null thì SP sẽ dùng CURDATE()).
    /// Chỉ lấy phần Date; Time bị bỏ qua.
    /// </summary>
    public DateTime? EventDate { get; set; }
}
