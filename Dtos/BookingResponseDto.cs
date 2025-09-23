namespace Backend_Nghiencf.DTOs
{
    public class BookingResponseDto
    {
        public int BookingId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentQrUrl { get; set; }
    }
}
