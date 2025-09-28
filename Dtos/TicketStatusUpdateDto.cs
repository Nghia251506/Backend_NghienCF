// Dtos/Ticket/TicketStatusUpdateDto.cs
namespace Backend_Nghiencf.Dtos.Ticket;

public sealed class TicketStatusUpdateDto
{
    /// <summary>
    /// ví dụ: "valid", "used", "invalid", ...
    /// </summary>
    public string Status { get; set; } = "valid";
}
