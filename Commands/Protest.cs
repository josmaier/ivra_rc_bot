using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using RaceControlBot.Data;
using RaceControlBot.Models;

namespace RaceControlBot.Commands
{
    public class ProtestCommand(DiscordSocketClient client, ApplicationDbContext db)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("protest", "Use to log a new protest")]
        public async Task ProtestAsync(
            [Summary("number", "What is your car number?"), MinValue(1)]
            int number,
            [Summary("numbers_involved", "Car numbers of other cars involved")]
            string numbersInvolved,
            [Summary("timestamp", "iRacing timestamp (HH:MM:SS)")]
            string timestamp,
            [Summary("description", "Short description of the incident")]
            string description
        )
        {
            await DeferAsync(ephemeral: false);

            string? protestChannelIdStr = Env.GetString("PROTEST_CHANNEL_ID");
            if (!ulong.TryParse(protestChannelIdStr, out ulong protestChannelId))
            {
                await FollowupAsync("Invalid or missing PROTEST_CHANNEL_ID in .env", ephemeral: true);
                return;
            }

            Embed? protestEmbed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("New protest")
                .WithDescription($"{this.Context.User.Mention} submitted a protest in {this.Context.Channel}")
                .AddField("Origin Car", number.ToString(), true)
                .AddField("Cars Involved", numbersInvolved, true)
                .AddField("Timestamp", timestamp, true)
                .AddField("Description", description, true)
                .WithCurrentTimestamp()
                .Build();

            Embed? confirmationEmbed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("Protest successfully submitted")
                .WithDescription($"Thank you {this.Context.User.Mention}, your protest is successfully submitted. Please check the protest sheet for the status.")
                .AddField("Protest Details", "Below you can find the information you submitted:", false)
                .AddField("Origin Car", number.ToString(), true)
                .AddField("Cars Involved", numbersInvolved, true)
                .AddField("Timestamp", timestamp, true)
                .AddField("Description", description, true)
                .WithCurrentTimestamp()
                .Build();

            if (client.GetChannel(protestChannelId) is not IMessageChannel protestChannel)
            {
                await FollowupAsync("Could not find the protest channel. Please check the configuration.", ephemeral: true);
                return;
            }

            IUserMessage? sentMessage = await protestChannel.SendMessageAsync("@here", embed: protestEmbed);

            Protest protest = new Protest
            {
                UserId = this.Context.User.Id,
                UserName = $"{this.Context.User.Username}#{this.Context.User.Discriminator}",
                CarNumber = number,
                CarsInvolved = numbersInvolved,
                TimeStampIR = timestamp,
                Description = description,
                ChannelId = protestChannelId,
                MessageId = sentMessage.Id,
                CreatedAt = DateTime.UtcNow,
                Served = false
            };

            db.Protests?.Add(protest);
            await db.SaveChangesAsync();

            await FollowupAsync(embed: confirmationEmbed, ephemeral: false);
        }
    }
}
