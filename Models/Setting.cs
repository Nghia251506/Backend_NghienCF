namespace Backend_Nghiencf.Models
{
    public class Setting
    {
        public int Id { get; set; }
        public string SettingKey { get; set; } = null!;
        public string? SettingValue { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
