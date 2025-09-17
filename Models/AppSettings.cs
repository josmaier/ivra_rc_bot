using System.ComponentModel.DataAnnotations;

namespace RaceControlBot.Models
{
    public class AppSetting
    {
        [Key] public int Id { get; set; }
        [MaxLength(100)] public string Key { get; set; } = string.Empty;
        [MaxLength(2000)] public string Value { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
