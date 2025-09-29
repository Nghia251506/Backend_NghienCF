namespace Backend_Nghiencf.Dtos.Ticket;

public sealed class TicketListItemDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string TicketCode { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime IssuedAt { get; set; }

    // từ Booking
    public string? CustomerName { get; set; }
    public string? Phone { get; set; }
    public DateTime? PaymentTime { get; set; }
    public int ShowId { get; set; }
}
