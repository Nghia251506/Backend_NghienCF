namespace Backend_Nghiencf.Dtos;

public sealed class TicketQuery
{
    // lọc
    public int? ShowId { get; set; }
    public string? TicketCode { get; set; }
    public DateTime? DateFrom { get; set; }   // lọc theo PaymentTime (nếu có), bằng UTC
    public DateTime? DateTo { get; set; }     // đã endOfDay ở FE thì giữ nguyên

    // phân trang
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
