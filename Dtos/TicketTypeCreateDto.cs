namespace Backend_Nghiencf.DTOs
{
    public class TicketTypeCreateDto
    {
        public int ShowId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }
    }
}