using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.IdentityModel.Tokens;

namespace RaceControlBot.Commands
{
    public class RCCommand(DiscordSocketClient client)
        : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("rc", "Use to send a message to race control")]
        public async Task RCAsync(
            [Summary("message", "Your message")] string message)
        {

            await DeferAsync(ephemeral: false);

            //This should not happen because they have to enter a message but you never know
            if (message.IsNullOrEmpty())
            {
                await FollowupAsync(text: "You entered a empty or invalid message");
            }

            Embed rcMessage = new EmbedBuilder()
                .WithTitle("New Message")
                .WithDescription($"{Context.User.Mention} sent a message in <#{Context.Channel.Id}>")
                .AddField("Message", message)
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .Build();

            Embed confirmationEmbed = new EmbedBuilder()
                .WithTitle("Your message has been sent to Race Control. They will reach out to you shortly if needed")
                .AddField("Your message", message)
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .Build();

            string? rcChannelIdStr = Env.GetString("RACE_CONTROL_CHANNEL_ID");
            if (!ulong.TryParse(rcChannelIdStr, out ulong rcChannelId))
            {
                await FollowupAsync("Invalid or missing RACE_CONTROL_CHANNEL_ID in .env", ephemeral: false);
                return;
            }

            if (client.GetChannel(rcChannelId) is not IMessageChannel rcChannel)
            {
                await FollowupAsync("Could not find the Race Control channel. Please check the configuration.", ephemeral: false);
                return;
            }

            await rcChannel.SendMessageAsync(text: "@here", embed: rcMessage);
            await FollowupAsync(embed: confirmationEmbed);

        }
    }
}
