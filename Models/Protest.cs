using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace RaceControlBot.Models
{
    /// <summary>
    /// Represents a single protest/log entry in Discord.
    /// Feel free to extend or subclass later to add more message types.
    /// </summary>
    public class Protest
    {
        [Key]
        public int Id { get; set; }

        // Discord user who submitted
        public ulong UserId { get; set; }
        [MaxLength(100)]
        public string UserName { get; set; }

        // The “fields” we care about for this protest
        public int CarNumber { get; set; }
        [MaxLength(200)]
        public string CarsInvolved { get; set; }
        [MaxLength(20)]
        public string TimeStampIR { get; set; }
        [MaxLength(500)]
        public string Description { get; set; }

        public string Penalty { get; set; }
        public Boolean Served { get; set; }
        public Boolean Published { get; set; }

        // Where the bot posted the embed
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }

        // When the row was added
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // public string ExtraField { get; set; }
    }
}
