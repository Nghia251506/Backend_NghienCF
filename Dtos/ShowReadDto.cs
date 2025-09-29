using Backend_Nghiencf.Models;

namespace Backend_Nghiencf.DTOs
{
    public class ShowReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset  Date { get; set; }
        public string? Location { get; set; }
        public string? BannerUrl { get; set; }
        public string? Capacity { get; set; }
        public string? Slogan { get; set; }
    }
}