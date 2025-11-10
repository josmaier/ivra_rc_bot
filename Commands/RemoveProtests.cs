using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceControlBot.Data;
using RaceControlBot.HelperFunctions;
using RaceControlBot.Models;

namespace RaceControlBot.Commands
{
    public class RemoveProtestsCommand(ApplicationDbContext db)
        : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("remove_protests", "Use to remove specific or all protests", runMode: RunMode.Async)]
        public async Task RemoveProtestsAsync(
            [Summary("protestIds", "The protests you want to delete, ids in comma separated list")]
            string protests,
            [Summary("boolean", "Do you want to remove all protests")]
            bool removeAllProtests = false)
        {
            await DeferAsync(ephemeral: false);

            try
            {
                if (protests.IsNullOrEmpty())
                {
                    await FollowupAsync("You did not enter any protests");
                    return;
                }

                List<int> protestList = protests
                    .Split([',', ';', '.', ' '], StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();

                if (db.Protests != null)
                {

                    List<Protest> protestsToBeDeleted;
                    if (!removeAllProtests)
                    {
                        protestsToBeDeleted = await db.Protests
                            .AsNoTracking()
                            .Where(p => protestList.Contains(p.Id))
                            .ToListAsync();
                    }
                    else
                    {
                        protestsToBeDeleted = await db.Protests.AsNoTracking().ToListAsync();
                    }

                    if (protestsToBeDeleted.Count == 0)
                    {
                        await FollowupAsync("No protests given or the DB does not hold any");
                        return;
                    }

                    List<Embed> embeds = BuildProtestEmbedsHelper.BuildProtestEmbeds(protestsToBeDeleted, this.Context.Guild?.Id);

                    /* if (embeds.Count > 0)
                    {
                        int total = protestsToBeDeleted.Count;

                        Embed header = new EmbedBuilder()
                            .WithTitle("The following protests have been deleted")
                            .WithDescription(
                                $"Total: **{total}**")
                            .WithColor(Color.Orange)
                            .WithTimestamp(DateTimeOffset.UtcNow)
                            .Build();

                        // send header + first chunk, then the rest
                        await FollowupAsync(embeds: [header, embeds[0]]);
                        for (int i = 1; i < embeds.Count; i++)
                        {
                            await FollowupAsync(embeds: [embeds[i]]);
                        }
                    } */

                    db.Protests.RemoveRange(protestsToBeDeleted);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
