namespace Backend_Nghiencf.Models
{
    public class TicketType
    {
        public int Id { get; set; }
        public int ShowId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public string? Description{ get; set; }
        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        // NEW: số ghế mà 1 vé loại này chiếm (S=1, SVip=1, Couple=2)
        public int SeatsUnit { get; set; }

        public Show Show { get; set; }
    }
}
