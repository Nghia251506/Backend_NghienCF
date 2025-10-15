using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_Nghiencf.Models
{
    public class Booking
    {
        public int Id { get; set; }

        
        public int ShowId { get; set; }

        
        public int TicketTypeId { get; set; }

        
        public string CustomerName { get; set; } = default!;

        
        public string Phone { get; set; } = default!;

        
        public int Quantity { get; set; }

        // VND nên có thể dùng decimal(18,0); nếu muốn bắt buộc scale=0, cấu hình trong OnModelCreating hoặc [Precision]
        
        public decimal TotalAmount { get; set; }

        
        public string PaymentStatus { get; set; } = "pending"; // pending|paid|failed

        public DateTime? PaymentTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ===== Thêm 3 trường tùy chọn để lưu tham chiếu phía Tingee (khắc phục lỗi build) =====
        [MaxLength(50)]
        public string? Provider { get; set; }                 // "tingee"
        [MaxLength(100)]
        public string? ProviderOrderId { get; set; }          // orderId bên Tingee (thường = Booking.Id.ToString())
        [MaxLength(100)]
        public string? ProviderTxnId { get; set; }            // transactionId nếu có

        // ===== Navigation =====
        public Show Show { get; set; } = default!;
        public TicketType TicketType { get; set; } = default!;

        // Gợi ý thêm: để FE dễ query vé sau khi paid
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
