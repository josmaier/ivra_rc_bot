using ATVO.RaceControl.Client;
using ATVO.RaceControl.Client.Messaging;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using RaceControlBot.Data;
using RaceControlBot.Models;
using System.Text.RegularExpressions;

namespace RaceControlBot.Commands
{
    public class ProtestCommand(DiscordSocketClient client, ApplicationDbContext db, RaceControlClient raceControlClient)
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
            string description,
            [Summary("session_type", "The session in which the incident occured")]
            IRacingSessionTypes? sessionType = null
        )
        {
            await DeferAsync(ephemeral: false);
            sessionType ??= IRacingSessionTypes.Race;
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
                TimeStampIr = timestamp,
                Description = description,
                Penalty = string.Empty,
                ChannelId = Context.Channel.Id,
                MessageId = 0,          // set after sending message
                CreatedAt = DateTime.UtcNow,
                Served = false
            };
            if (!Program.IVRA)
            {
                if (db.Protests == null)
                {
                    await FollowupAsync("Why the fuck is there no database");
                    return;
                }
                db.Protests.Add(protest);
                await db.SaveChangesAsync();
            }

            if (Program.ATVO_RC)
            {
                List<string> all = Regex.Matches(numbersInvolved, @"\d+")
                    .Select(m => int.Parse(m.Value))
                    .Append(number)          // include origin
                    .Distinct()              // remove duplicates
                    .OrderBy(n => n) // sort ascending
                    .Select(t => t.ToString())
                    .ToList();

                string cars = string.Join(",", all);

                RemoteRaceControlInvestigation investigation = new RemoteRaceControlInvestigation()
                {
                    IdType = RaceControlIncidentEntryIdTypes.CarNumber,
                    Entries = all,
                    SessionName = sessionType.ToString(),
                    SessionTime = 1
                };

                RemoteRaceControlIncidentResult res = await raceControlClient.StartInvestigation(investigation);
                Console.WriteLine(res.Success);
                Console.WriteLine(res.Message);

                PrintIncidents(res.Incidents);
            }


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
            db.Protests?.Update(protest);
            await db.SaveChangesAsync();

            await FollowupAsync(embed: confirmationEmbed, ephemeral: false);
        }

        public static void PrintIncidents(IEnumerable<RaceControlIncidentResponse> incidents)
        {
            if (incidents == null)
            {
                Console.WriteLine("No incidents rn");
                return;
            }
            foreach (RaceControlIncidentResponse i in incidents)
            {
                Console.WriteLine(
                    $@"IncidentId : {i.IncidentId}
                    GroupId    : {i.GroupId}
                    CarIdx     : {i.CarIdx}
                    SessionName: {i.SessionName}
                    Decision   : {i.Decision}
                    Penalty    : {i.Penalty}
                    IsServed   : {i.IsServed}
                    IsActive   : {i.IsActive}
                    SessionTime: {i.SessionTime}
                    Timestamp  : {i.Timestamp}
                    ---------------------------");
            }
        }
    }

    public enum IRacingSessionTypes
    {
        Practice,
        Qualifying,
        Race
    }
}
