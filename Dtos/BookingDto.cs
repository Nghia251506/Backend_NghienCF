namespace Backend_Nghiencf.DTOs
{
    public class BookingDto
    {
        public int ShowId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public int TicketTypeId { get; set; }
        public int Quantity { get; set; }
        public int remainningQuantity { get; set; }
    }
}
