using System.ComponentModel.DataAnnotations;

namespace Backend_Nghiencf.Models
{
    public class Show
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; }
        public string? Description { get; set; }
        [Required] public DateTime Date { get; set; }
        public string? Location { get; set; }
        public string? BannerUrl { get; set; }
        public string? Capacity { get; set; }
        public string? Slogan { get; set; }
        public string? IsDefault { get; set; } = "Inactive";
        public string? DeleteStatus { get; set; } = "Active";
         // NEW: tổng ghế & ghế còn lại (đi theo seats, không theo vé)
        public int TotalSeats { get; set; }        // total_seats
        public int RemainingSeats { get; set; } // remaining_seats (có thể .0/.5 tùy factor, dùng DECIMAL)

        // public static implicit operator Show(Show v)
        // {
        //     throw new NotImplementedException();
        // }
    }
}
