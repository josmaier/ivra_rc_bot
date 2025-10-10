using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using RaceControlBot.Data;
using RaceControlBot.Models;
using static RaceControlBot.HelperFunctions.BuildProtestEmbedsHelper;

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

            if (Program.rcOnlyCommandRoleList == null)
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
                await FollowupAsync(embeds: [header, embeds[0]]);
                for (int i = 1; i < embeds.Count; i++)
                {
                    await FollowupAsync(embeds: [embeds[i]]);
                }
            }
            else
            {
                await FollowupAsync("No protests to display.", ephemeral: true);
            }
        }
    }
}


