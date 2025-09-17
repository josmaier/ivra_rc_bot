using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace RaceControlBot.Commands
{
    public class TeamCommand()
        : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("team", "Send a message to the given team channel")]
        public async Task TeamAsync(
            [Summary("channel", "The channel to send the message to")] ITextChannel teamChannel,
            [Summary("message", "The message you want to send")] string message,
            [Summary("tag", "Role you want to mention with your message")] IRole? roleToTag = null)
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

            Embed teamMessage = new EmbedBuilder()
                .WithTitle("Race Control sent a message")
                .WithDescription(message)
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .Build();

            Embed confirmationMessage = new EmbedBuilder()
                .WithTitle($"You have sent the following message to <#{teamChannel.Id}>")
                .WithDescription(message)
                .WithColor(Color.Green)
                .WithCurrentTimestamp() 
                .Build();

            if (roleToTag != null)
            {
                await teamChannel.SendMessageAsync(text: roleToTag.Mention,embed: teamMessage);
            } else
            {
                await teamChannel.SendMessageAsync(embed: teamMessage);
            }
                await FollowupAsync(text: "Your message has been sent", embed: confirmationMessage);
        }
    }
}
