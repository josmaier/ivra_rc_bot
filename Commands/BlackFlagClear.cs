using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;

namespace RaceControlBot.Commands
{
    public class BlackFlagClearRequestCommand(DiscordSocketClient client)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("clear_black_flag", "Use to request a black flag being cleared")]
        public async Task RequestBlackFlagClearAsync(
            [Summary("number", "Your car number")] int requestingCarNumber,
            [Summary("lap", "The lap you recieved the black flag on")] int recievingLapNumber,
            [Summary("reason", "Why did you recieve the black flag")] string reason)
        {
            await DeferAsync(ephemeral: false);

            Embed message = new EmbedBuilder()
                .WithTitle("New Black Flag Clear Request")
                .WithDescription($"{Context.User.Mention} requested a black flag clear in <#{Context.Channel.Id}>")
                .AddField("Requesting Car", requestingCarNumber.ToString())
                .AddField("Lap", recievingLapNumber.ToString())
                .AddField("Reason", reason)
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .Build();

            Embed confirmationEmbed = new EmbedBuilder()
                .WithTitle("You requested a black flag clear")
                .WithDescription("Below is the information you submitted")
                .AddField("Requesting Car", requestingCarNumber.ToString())
                .AddField("Lap", recievingLapNumber.ToString())
                .AddField("Reason", reason)
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .Build();

            string? blackFlagClearChannelIdStr = Env.GetString("BF_CLEAR_CHANNEL_ID");
            if (!ulong.TryParse(blackFlagClearChannelIdStr, out ulong bfClearChannelId))
            {
                await FollowupAsync("Invalid or missing BF_CLEAR_CHANNEL_ID in .env", ephemeral: false);
                return;
            }

            if (client.GetChannel(bfClearChannelId) is not IMessageChannel bfClearChannel)
            {
                await FollowupAsync("Could not find the protest channel. Please check the configuration.", ephemeral: false);
                return;
            }

            
            await bfClearChannel.SendMessageAsync(text: "@here", embed: message);
            await FollowupAsync(embed: confirmationEmbed);
        }
    }
}