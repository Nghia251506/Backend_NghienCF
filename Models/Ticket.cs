namespace Backend_Nghiencf.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string TicketCode { get; set; }
        public string Status { get; set; } = "valid";
        public DateTime IssuedAt { get; set; } = DateTime.Now;

        public Booking Booking { get; set; }
    }
}
