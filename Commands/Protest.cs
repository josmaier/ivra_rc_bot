using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
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

            Protest protest = new Protest
            {
                UserId = Context.User.Id,
                UserName = $"{Context.User.Username}#{Context.User.Discriminator}",
                CarNumber = number,
                CarsInvolved = numbersInvolved,
                TimeStampIR = timestamp,
                Description = description,
                Penalty = string.Empty,
                ChannelId = Context.Channel.Id,
                MessageId = 0,          // set after sending message
                CreatedAt = DateTime.UtcNow,
                Served = false
            };

            db.Protests.Add(protest);
            await db.SaveChangesAsync();

            Embed? protestEmbed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("New protest")
                .WithDescription($"{Context.User.Mention} submitted a protest in <#{Context.Channel.Id}>")
                .AddField("Origin Car", number.ToString(), true)
                .AddField("Cars Involved", numbersInvolved, true)
                .AddField("Timestamp", timestamp, true)
                .AddField("Protest ID", protest.Id, true)
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
                .AddField("Your Protest ID", protest.Id, true)
                .WithCurrentTimestamp()
                .Build();

            if (client.GetChannel(protestChannelId) is not IMessageChannel protestChannel)
            {
                await FollowupAsync("Could not find the protest channel. Please check the configuration.", ephemeral: true);
                return;
            }

            IUserMessage? sentMessage = await protestChannel.SendMessageAsync("@here", embed: protestEmbed);

            protest.MessageId = sentMessage.Id;
            db.Protests.Update(protest);
            await db.SaveChangesAsync();

            await FollowupAsync(embed: confirmationEmbed, ephemeral: false);
        }
    }
}
