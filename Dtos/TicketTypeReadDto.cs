namespace Backend_Nghiencf.Models
{
    public class TicketTypeReadDto
    {
        public int Id { get; set; }
        public int ShowId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }

        public Show Show { get; set; }
    }
}