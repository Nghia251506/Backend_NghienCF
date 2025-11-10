// Dtos/Ticket/TicketReadDto.cs
using Backend_Nghiencf.Models;

namespace Backend_Nghiencf.Dtos.Ticket;

public sealed class TicketReadDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string TicketCode { get; set; } = "";
    public string Status { get; set; } = "valid";
    public DateTime IssuedAt { get; set; }
    public Booking booking { get; set; }
}
