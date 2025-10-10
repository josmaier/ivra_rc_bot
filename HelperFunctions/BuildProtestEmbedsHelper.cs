using Discord;
using RaceControlBot.Models;
using System.Text;

namespace RaceControlBot.HelperFunctions
{
    public class BuildProtestEmbedsHelper()
    {

        /// <summary>
        /// This builds a list of embeds from a given list of protests
        /// </summary>
        /// <param name="protests">List of protest objects</param>
        /// <param name="guildId">The id of the guild that the command was called in</param>
        /// <returns>List of embeds</returns>
        public static List<Embed> BuildProtestEmbeds(List<Protest> protests, ulong? guildId)
        {
            List<Embed> result = new List<Embed>();
            EmbedBuilder builder = new EmbedBuilder().WithColor(Color.Orange);

            int fieldCount = 0;
            foreach (Protest p in protests)
            {
                string fieldName = BuildFieldName(p);
                string fieldValue = BuildFieldValue(p, guildId);

                fieldName = Truncate(fieldName, 256);
                fieldValue = Truncate(fieldValue, 1024);

                builder.AddField(fieldName, fieldValue, inline: false);
                fieldCount++;

                if (fieldCount < 20)
                {
                    continue;
                }

                result.Add(builder.Build());
                builder = new EmbedBuilder().WithColor(Color.Orange);
                fieldCount = 0;
            }

            if (fieldCount > 0)
                result.Add(builder.Build());

            return result;
        }

        /// <summary>
        /// This function takes the protest P and returns a string with information on the protest to be displayed
        /// </summary>
        /// <param name="p">Protest object</param>
        /// <returns>String</returns>
        private static string BuildFieldName(Protest p)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('#').Append(p.Id)
              .Append(" — Car ").Append(p.CarNumber);

            sb.Append(" • ").Append(p.Published ? "Published" : "Unpublished");
            if (p.Served)
            {
                sb.Append(" • Served");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Takes in a protest and the guild id to build the value of the field, this is information about the protest and a link to the message where it was sent
        /// </summary>
        /// <param name="p">Protest object</param>
        /// <param name="guildId">Guild in which the command was used</param>
        /// <returns>String containing formatted information on the protest</returns>
        private static string BuildFieldValue(Protest p, ulong? guildId)
        {
            // Link to the original message if we have enough info
            string? link = null;
            if (guildId.HasValue && p.ChannelId != 0 && p.MessageId != 0)
            {
                link = $"https://discord.com/channels/{guildId}/{p.ChannelId}/{p.MessageId}";
            }

            StringBuilder sb = new StringBuilder();

            // Reporter
            if (p.UserId != 0)
            {
                sb.Append("Reporter: ").Append(p.UserName)
                  .Append(" (").Append("<@").Append(p.UserId).Append('>').Append(')').AppendLine();
            }
            else
            {
                sb.Append("Reporter: ").Append(p.UserName).AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(p.CarsInvolved))
                sb.Append("Cars involved: ").Append(Truncate(p.CarsInvolved, 180)).AppendLine();

            if (!string.IsNullOrWhiteSpace(p.TimeStampIr))
                sb.Append("iR Timestamp: ").Append(Truncate(p.TimeStampIr, 60)).AppendLine();

            if (!string.IsNullOrWhiteSpace(p.Description))
                sb.Append("Description: ").Append(Truncate(p.Description, 600)).AppendLine();

            sb.Append("Created (UTC): ").Append(p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).AppendLine();

            if (link != null)
                sb.Append("[Jump to message](").Append(link).Append(')');

            return sb.ToString().TrimEnd();
        }

        private static string Truncate(string value, int max)
            => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
    }
}
