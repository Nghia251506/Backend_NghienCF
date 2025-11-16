namespace Backend_Nghiencf.DTOs
{
    public class TicketTypeUpdateDto
    {
        public int ShowId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public string? Description{ get; set; }
        public int TotalQuantity { get; set; } = int.MaxValue;
        public int RemainingQuantity{ get; set; }
        public int SeatUnit { get; set; } = 1;     // 👈 cập nhật được
        
    }
}