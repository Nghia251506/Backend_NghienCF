namespace Backend_Nghiencf.DTOs
{
    public class ShowCreateDto
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime  Date { get; set; }
        public string? Location { get; set; }
        public string? BannerUrl { get; set; }
        public string? Capacity { get; set; }
        public string? Slogan { get; set; }
    }
}