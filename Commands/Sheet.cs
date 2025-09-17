using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceControlBot.Data;
using RaceControlBot.Models;

namespace RaceControlBot.Commands
{
    public class SheetCommand(ApplicationDbContext db)
        : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("sheet", "Brings up a link to the Race Control notice board")]
        public async Task SheetAsync()
        {

            await DeferAsync(ephemeral: false);
            if (Program.sheetURL.IsNullOrEmpty())
            {
                await FollowupAsync(text: "There is no sheet URL set, if you think this is an error contact RC");
                return;
            }
            await FollowupAsync(text: $"You can find the sheet here: \n {Program.sheetURL}");
        }

        [SlashCommand("update-sheet", "Update the sheet url the bot returns")]
        public async Task UpdateSheetURLAsync(
            [Summary("url", "The sheet url you want to set")] string url)
        {
            await DeferAsync(ephemeral: false);

            SocketGuildUser member = (SocketGuildUser)Context.User;
            if (Program.rcOnlyCommandRoleList == null)
            {
                await FollowupAsync("Role configuration missing.");
                return;
            }
            if (!HelperFunctions.RoleCheck.HasRoles(member, Program.rcOnlyCommandRoleList))
            {
                await FollowupAsync("You do not have the permissions required to run this command", ephemeral: false);
                return;
            }

            AppSetting? setting = await db.Settings
                .FirstOrDefaultAsync(s => s.Key == "SheetUrl");

            if (setting == null)
            {
                setting = new AppSetting { Key = "SheetUrl", Value = url, UpdatedAt = DateTime.UtcNow };
                db.Settings.Add(setting);
            }
            else
            {
                setting.Value = url;
                setting.UpdatedAt = DateTime.UtcNow;
                db.Settings.Update(setting);
            }

            await db.SaveChangesAsync();

            Program.sheetURL = url;

            await FollowupAsync(text: $"Successfully set the sheet url to {url}");
        }
    }
}
