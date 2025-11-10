using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_Nghiencf.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public string? BookingCode {get;set;}

        [Required]
        public int ShowId { get; set; }

        [Required]
        public int TicketTypeId { get; set; }

        [Required, MaxLength(200)]
        public string CustomerName { get; set; } = default!;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = default!;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // VND nên có thể dùng decimal(18,0); nếu muốn bắt buộc scale=0, cấu hình trong OnModelCreating hoặc [Precision]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required, MaxLength(32)]
        public string PaymentStatus { get; set; } = "pending"; // pending|paid|failed

        public DateTime? PaymentTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ===== Thêm 3 trường tùy chọn để lưu tham chiếu phía Tingee (khắc phục lỗi build) =====
        [NotMapped] public string? Provider { get; set; }
        [NotMapped] public string? ProviderOrderId { get; set; }
        [NotMapped] public string? ProviderTxnId { get; set; }

        // ===== Navigation =====
        public Show Show { get; set; } = default!;
        public TicketType TicketType { get; set; } = default!;


        // Gợi ý thêm: để FE dễ query vé sau khi paid
        // public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
