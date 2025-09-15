using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.IdentityModel.Tokens;

namespace RaceControlBot.Commands
{
    public class SheetCommand(DiscordSocketClient client)
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
            if (!HelperFunctions.RoleCheck.HasRoles(member, Program.rcOnlyCommandRoleList))
            {
                await FollowupAsync("You do not have the permissions required to run this command", ephemeral: false);
                return;
            }
            Program.sheetURL = url;
            await FollowupAsync(text: $"Successfully set the sheet url to {url}");
        }
    }
}
