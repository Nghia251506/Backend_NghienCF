namespace Backend_Nghiencf.DTOs
{
    public class BookingResponseDto
    {
        public int BookingId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentQrUrl { get; init; }     // nếu có
        public string? PaymentQrImage { get; init; }   // data:image/png;base64,...
        public string? PaymentQrString { get; init; }
    }
}
