namespace Backend_Nghiencf.DTOs
{
    public class SettingCreateDto
    {
        public string SettingKey { get; set; } = null!;
        public string? SettingValue { get; set; }
    }

    public class SettingReadDto
    {
        public int Id { get; set; }
        public string SettingKey { get; set; } = null!;
        public string? SettingValue { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SettingUpdateDto
    {
        public string? SettingValue { get; set; }
    }
}
