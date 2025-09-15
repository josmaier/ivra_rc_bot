using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using RaceControlBot.Data;
using RaceControlBot.Models;
using System.Text;

namespace RaceControlBot.Commands
{
    public class ListProtestsCommand(ApplicationDbContext db)
       : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("listprotests", "Use to list all active protests", runMode: RunMode.Async)]
        public async Task ListProtestsAsync(
            [Summary("boolean", "Do you want to include already published protests?")]
            bool displayPublished
        )
        {
            await DeferAsync(ephemeral: false);
            SocketGuildUser member = (SocketGuildUser)Context.User;

            if(Program.rcOnlyCommandRoleList == null)
            {
                await FollowupAsync("Why the fuck is the main program gone?");
                return;
            }
            if (!HelperFunctions.RoleCheck.HasRoles(member, Program.rcOnlyCommandRoleList))
            {
                await FollowupAsync("You do not have the permissions required to run this command", ephemeral: false);
                return;
            }

            if (db.Protests == null)
            {
                await FollowupAsync("Why the fuck is there no database");
                return;
            }

            bool any = await db.Protests.AsNoTracking().AnyAsync();
            if (!any)
            {
                await FollowupAsync("There are no protests logged at this point.");
                return;
            }

            List<Protest> protests = await db.Protests
                .AsNoTracking()
                .Where(p => displayPublished || !p.Published)
                .ToListAsync();

            // Build embeds in chunks of up to 25 fields each
            List<Embed> embeds = BuildProtestEmbeds(protests, Context.Guild?.Id);

            // First embed gets a summary header
            if (embeds.Count > 0)
            {
                int total = protests.Count;
                int published = protests.Count(p => p.Published);
                int served = protests.Count(p => p.Served);
                int unpublished = total - published;

                Embed header = new EmbedBuilder()
                    .WithTitle("Protest Log")
                    .WithDescription(
                        $"Total: **{total}** • Unpublished: **{unpublished}** • Published: **{published}** • Served: **{served}**" +
                        (displayPublished ? "" : "\n(Showing only unpublished; pass `true` to include published.)"))
                    .WithColor(Color.Orange)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .Build();

                // send header + first chunk, then the rest
                await FollowupAsync(embeds: new[] { header, embeds[0] });
                for (int i = 1; i < embeds.Count; i++)
                {
                    await FollowupAsync(embeds: new[] { embeds[i] });
                }
            }
            else
            {
                await FollowupAsync("No protests to display.", ephemeral: true);
            }
        }

        private static List<Embed> BuildProtestEmbeds(List<Protest> protests, ulong? guildId)
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

                if (fieldCount >= 20)
                {
                    result.Add(builder.Build());
                    builder = new EmbedBuilder().WithColor(Color.Orange);
                    fieldCount = 0;
                }
            }

            if (fieldCount > 0)
                result.Add(builder.Build());

            return result;
        }

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

        private static string BuildFieldValue(Protest p, ulong? guildId)
        {
            // Link to the original message if we have enough info
            string? link = null;
            if (guildId.HasValue && p.ChannelId != 0 && p.MessageId != 0)
            {
                link = $"https://discord.com/channels/{guildId}/{p.ChannelId}/{p.MessageId}";
            }

            var sb = new StringBuilder();

            // Reporter
            if (p.UserId != 0)
            {
                sb.Append("Reporter: ").Append(p.UserName)
                  .Append(" (").Append("<@").Append(p.UserId).Append(">").Append(')').AppendLine();
            }
            else
            {
                sb.Append("Reporter: ").Append(p.UserName).AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(p.CarsInvolved))
                sb.Append("Cars involved: ").Append(Truncate(p.CarsInvolved, 180)).AppendLine();

            if (!string.IsNullOrWhiteSpace(p.TimeStampIR))
                sb.Append("iR Timestamp: ").Append(Truncate(p.TimeStampIR, 60)).AppendLine();

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


