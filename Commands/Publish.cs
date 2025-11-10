using ATVO.RaceControl.Client;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using RaceControlBot.Data;
using RaceControlBot.Models;

namespace RaceControlBot.Commands
{
    public class PublishCommand(DiscordSocketClient client, ApplicationDbContext db, RaceControlClient raceControl)
    : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("publish", "Publish a penalty to the team, notice board and ATVO Server")]
        public async Task PublishAsync(
            [Summary("message", "The message to publish")] string message,
            [Summary("team-channel", "Channel to notify team")] ITextChannel teamChannel,
            [Summary("tag-role", "Role to mention in team channel")] IRole roleToTag,
            [Summary("id", "Incident Number")] int protestId)
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
            // Load protest from DB
            Protest? protest = await db.Protests.FindAsync(protestId);
            if (protest == null)
            {
                protest = new Protest
                {
                    UserId = Context.User.Id,
                    UserName = $"{Context.User.Username}#{Context.User.Discriminator}",
                    CarNumber = 0,
                    CarsInvolved = string.Empty,
                    TimeStampIr = string.Empty,
                    Description = string.Empty,
                    Penalty = message,
                    Served = false,
                    Published = false,
                    ChannelId = 0,
                    MessageId = 0,
                    CreatedAt = DateTime.UtcNow
                };

                db.Protests.Add(protest);
                await db.SaveChangesAsync();

                // ensure the rest of the command references the actual new ID
                protestId = protest.Id;

            }
            if (protest.Published == true)
            {
                await FollowupAsync($"Protest with ID {protestId} has already been published", ephemeral: false);
                return;
            }


            Embed embed = new EmbedBuilder()
                .WithTitle("Penalty Notice")
                .WithDescription(message)
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .Build();

            Embed teamEmbed = new EmbedBuilder()
                .WithTitle("Penalty Notice")
                .WithDescription(message)
                .AddField("Your Protest ID", protestId)
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .Build();

            Embed confirmationEmbed = new EmbedBuilder()
                .WithTitle("Penalty Published")
                .AddField("Protest ID", protestId)
                .AddField("Destination Channel", teamChannel)
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .Build();

            if (client.GetChannel(noticeBoardId) is not IMessageChannel noticeBoard)
            {
                await FollowupAsync("Could not find the protest channel. Please check the configuration.", ephemeral: false);
                return;
            }

            await teamChannel.SendMessageAsync(text: roleToTag.Mention, embed: teamEmbed);

            await noticeBoard.SendMessageAsync(embed: embed);

            protest.Published = true;
            protest.Penalty = message;
            db.Protests.Update(protest);
            await db.SaveChangesAsync();

            await FollowupAsync(embed: confirmationEmbed, ephemeral: false);
        }
    }
}
