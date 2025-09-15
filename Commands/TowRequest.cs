
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;

namespace RaceControlBot.Commands
{
    public class TowRequestCommand(DiscordSocketClient client)
        : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("request_tow", "Use to request a tow from RC")]
        public async Task RequestTowAsync(
            [Summary("number", "Your car number")] int number)
        {

            await DeferAsync(ephemeral: false);

            Embed message = new EmbedBuilder()
                .WithTitle("New Tow Request")
                .WithDescription($"{Context.User.Mention} requested a tow in <#{Context.Channel.Id}>")
                .AddField("Origin Car", number.ToString())
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .Build();

            string? towRequestChannelIdStr = Env.GetString("TOW_CHANNEL_ID");
            if (!ulong.TryParse(towRequestChannelIdStr, out ulong towRequestChannelId))
            {
                await FollowupAsync("Invalid or missing TOW_CHANNEL_ID in .env", ephemeral: false);
                return;
            }

            if (client.GetChannel(towRequestChannelId) is not IMessageChannel towRequestChannel)
            {
                await FollowupAsync("Could not find the protest channel. Please check the configuration.", ephemeral: false);
                return;
            }

            await towRequestChannel.SendMessageAsync(text: "@here", embed: message);
            await FollowupAsync(text: "Tow successfully requested.  Please wait for RC confirmation before you tow back to the pits!");
        }
    }
}
