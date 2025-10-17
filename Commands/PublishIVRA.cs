using ATVO.RaceControl.Client;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using RaceControlBot.Data;
using RaceControlBot.Models;
using System.Text.RegularExpressions;

namespace RaceControlBot.Commands
{
    public class PublishCommandIVRA(DiscordSocketClient client, ApplicationDbContext db, RaceControlClient raceControl) 
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("publishivra", "Publish a penalty to the team, notice board and ATVO Server")]
        public async Task PublishIVRAAsync(
            [Summary("penalty", "The penalty")] 
            string penalty,
            [Summary("team-channel", "Channel to notify team")] 
            ITextChannel teamChannel,
            [Summary("tag-role", "Role to mention in team channel")] 
            IRole roleToTag,
            [Summary("id", "Protest Id")] 
            int protestId,
            [Summary("number", "What is your car number?"), MinValue(1)] 
            int number,
            [Summary("timestamp", "iRacing timestamp (HH:MM:SS)")] 
            string timestamp,
            [Summary("category", "The Incident category")] 
            IncidentCategory category,
            [Summary("session_type", "The session in which the incident occured")] 
            IRacingSessionTypes? sessionType = null)
        {
            await DeferAsync(ephemeral: false);
            SocketGuildUser member = (SocketGuildUser)Context.User;
            if (!Program.IVRA)
            {
                await FollowupAsync("This command is not available in the current environment.", ephemeral: true);
                return;
            }

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
            string? noticeBoardChannelIdStr = Env.GetString("NOTICE_BOARD_CHANNEL_ID");
            if (!ulong.TryParse(noticeBoardChannelIdStr, out ulong noticeBoardId))
            {
                await FollowupAsync("Invalid or missing NOTICE_BOARD_CHANNEL_ID in .env", ephemeral: false);
                return;
            }

            if (db.Protests == null)
            {
                await FollowupAsync("Why the fuck is there no database");
                return;
            }

            if (db.Protests.Any(p => p.Id == protestId))
            {
                await FollowupAsync("This protest ID already exists!");
                return;
            }
            Protest protest = new Protest
            {
                Id = protestId,
                UserId = Context.User.Id,
                UserName = $"{Context.User.Username}#{Context.User.Discriminator}",
                CarNumber = number,
                CarsInvolved = string.Empty,
                TimeStampIr = timestamp,
                Description = string.Empty,
                Penalty = penalty,
                ChannelId = Context.Channel.Id,
                MessageId = 0, // set after sending message
                CreatedAt = DateTime.UtcNow,
                Served = false,
                Published = false
            };

            db.Protests.Add(protest);
            await db.SaveChangesAsync();


            string incidentCategory = (typeof(IncidentCategory)
                .GetField(category.ToString())!
                .GetCustomAttributes(typeof(ChoiceDisplayAttribute), false)
                .Cast<ChoiceDisplayAttribute>()
                .FirstOrDefault()?
                .Name ?? category.ToString())
                .Split('-', 2)[1]
                .Trim();

            Console.WriteLine(incidentCategory);

            string teamMessage = $"{penalty} for Car {number} for Inc. {protestId}." +
                                 "\n You have 90 minutes to serve the penalty. Please acknowledge you have seen this message with a reaction.";
            
            string noticeBoardMessage = $"**Inc. {protestId}** - {incidentCategory} - {penalty} for Car {number}";


            Embed embed = new EmbedBuilder()
                .WithTitle("Penalty Notice")
                .WithDescription(noticeBoardMessage)
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .Build();

            Embed teamEmbed = new EmbedBuilder()
                .WithTitle("Penalty Notice")
                .WithDescription(teamMessage)
                //.AddField("Category", incidentCategory)
                .AddField("Your Protest ID", protestId)
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .Build();

            Embed confirmationEmbed = new EmbedBuilder().WithTitle("Penalty Published")
                .AddField("Protest ID", protestId)
                .AddField("Destination Channel", teamChannel)
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .Build();

            if (client.GetChannel(noticeBoardId) is not IMessageChannel noticeBoard)
            {
                await FollowupAsync("Could not find the protest channel. Please check the configuration.", ephemeral: false); return;
            }

            await teamChannel.SendMessageAsync(text: roleToTag.Mention, embed: teamEmbed);
            await noticeBoard.SendMessageAsync(embed: embed);

            protest.Published = true;
            db.Protests.Update(protest);
            await db.SaveChangesAsync();
            await FollowupAsync(embed: confirmationEmbed, ephemeral: false);
        }
    }
    public enum IncidentCategory
    {
        [ChoiceDisplay("1.1 - Incorrect Car in session")] 
        IncorrectCarInSession = 11,
        [ChoiceDisplay("1.2 - Incorrect TeamID/Number in session")] 
        IncorrectTeamIDOrNumber = 12,
        [ChoiceDisplay("1.3 - Non-registered Driver")] 
        NonRegisteredDriver = 13,
        [ChoiceDisplay("1.4 - Drive time infringement")] 
        DriveTimeInfringement = 14,
        [ChoiceDisplay("2.1 - Qualifying infringement")] 
        QualifyingInfringement = 21,
        [ChoiceDisplay("2.2 - Start infringement")] 
        StartInfringement = 22,
        [ChoiceDisplay("2.3 - Pit Lane infringement")] 
        PitLaneInfringement = 23,
        [ChoiceDisplay("2.4 - Pit Lane Safety Car infringement")] 
        PitLaneSafetyCarInfringement = 24,
        [ChoiceDisplay("2.5 - Safety Car Procedure infringement")] 
        SafetyCarProcedureInfringement = 25,
        [ChoiceDisplay("2.5.A - Safety Car Procedure infringement (A)")] 
        SafetyCarProcedureInfringementA = 251,
        [ChoiceDisplay("2.6 - Restart infringement")] 
        RestartInfringement = 26,
        [ChoiceDisplay("3.1 - Minor incident responsibility")] 
        MinorIncident = 31,
        [ChoiceDisplay("3.2 - Moderate incident responsibility")] 
        ModerateIncident = 32,
        [ChoiceDisplay("3.3 - Major incident responsibility")] 
        MajorIncident = 33,
        [ChoiceDisplay("3.4 - Blocking")] 
        Blocking = 34,
        [ChoiceDisplay("3.5 - Not respecting track limits")] 
        TrackLimits = 35,
        [ChoiceDisplay("3.6 - Overtaking outside track limits")] 
        OvertakeOutsideTrack = 36,
        [ChoiceDisplay("3.7 - Unsafe rejoin")] 
        UnsafeRejoin = 37,
        [ChoiceDisplay("3.8 - Aggressive driving")] 
        AggressiveDriving = 38,
        [ChoiceDisplay("4.1 - Unsportsmanlike conduct")] 
        UnsportsmanlikeConduct = 41
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                // CreatedAt = DateTime.UtcNow, Served = false }; Embed embed = new EmbedBuilder() .WithTitle("Penalty Notice") .WithDescription(message) .WithColor(Color.Orange) .WithCurrentTimestamp() .Build(); Embed teamEmbed = new EmbedBuilder() .WithTitle("Penalty Notice") .WithDescription(message) .AddField("Your Protest ID", protestId) .WithColor(Color.Orange) .WithCurrentTimestamp() .Build(); Embed confirmationEmbed = new EmbedBuilder() .WithTitle("Penalty Published") .AddField("Protest ID", protestId) .AddField("Destination Channel", teamChannel) .WithColor(Color.Green) .WithCurrentTimestamp() .Build(); if (client.GetChannel(noticeBoardId) is not IMessageChannel noticeBoard) { await FollowupAsync("Could not find the protest channel. Please check the configuration.", ephemeral: false); return; } await teamChannel.SendMessageAsync(text: roleToTag.Mention, embed: teamEmbed); await noticeBoard.SendMessageAsync(embed: embed); protest.Published = true; db.Protests.Add(protest); await db.SaveChangesAsync(); await FollowupAsync(embed: confirmationEmbed, ephemeral: false); } } public enum IncidentCategory { [ChoiceDisplay("1.1 – Incorrect Car in session")] IncorrectCarInSession = 11, [ChoiceDisplay("1.2 – Incorrect TeamID/Number in session")] IncorrectTeamIDOrNumber = 12, [ChoiceDisplay("1.3 – Non-registered Driver")] NonRegisteredDriver = 13, [ChoiceDisplay("1.4 – Drive time infringement")] DriveTimeInfringement = 14, [ChoiceDisplay("2.1 – Qualifying infringement")] QualifyingInfringement = 21, [ChoiceDisplay("2.2 – Start infringement")] StartInfringement = 22, [ChoiceDisplay("2.3 – Pit Lane infringement")] PitLaneInfringement = 23, [ChoiceDisplay("2.4 – Pit Lane Safety Car infringement")] PitLaneSafetyCarInfringement = 24, [ChoiceDisplay("2.5 – Safety Car Procedure infringement")] SafetyCarProcedureInfringement = 25, [ChoiceDisplay("2.5.A – Safety Car Procedure infringement (A)")] SafetyCarProcedureInfringementA = 251, // “A” can’t be numeric, so use suffix or fake int [ChoiceDisplay("2.6 – Restart infringement")] RestartInfringement = 26, [ChoiceDisplay("3.1 – Minor incident responsibility")] MinorIncident = 31, [ChoiceDisplay("3.2 – Moderate incident responsibility")] ModerateIncident = 32, [ChoiceDisplay("3.3 – Major incident responsibility")] MajorIncident = 33, [ChoiceDisplay("3.4 – Blocking")] Blocking = 34, [ChoiceDisplay("3.5 – Not respecting track limits")] TrackLimits = 35, [ChoiceDisplay("3.6 – Overtaking outside track limits")] OvertakeOutsideTrack = 36, [ChoiceDisplay("3.7 – Unsafe rejoin")] UnsafeRejoin = 37, [ChoiceDisplay("3.8 – Aggressive driving")] AggressiveDriving = 38, [ChoiceDisplay("4.1 – Unsportsmanlike conduct")] UnsportsmanlikeConduct = 41 } }