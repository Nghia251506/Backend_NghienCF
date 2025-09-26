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
    }
}
