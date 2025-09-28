namespace Backend_Nghiencf.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int ShowId { get; set; }
        public int TicketTypeId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = "pending";
        public DateTime? PaymentTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // <-- THÊM

        public Show Show { get; set; }
        public TicketType TicketType { get; set; }
    }
}
