using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using RaceControlBot.Data;
using RaceControlBot.Models;
using System.Data;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RaceControlBot.Commands
{
    public class PublishCommand(DiscordSocketClient client, ApplicationDbContext db)
    : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("publish", "Publish a penalty to the team, notice board and ATVO Server")]
        public async Task PublishAsync(
            [Summary("message", "The message to publish")] string message,
            [Summary("team-channel", "Channel to notify team")] ITextChannel teamChannel,
            [Summary("tag-role", "Role to mention in team channel")] IRole roleToTag,
            [Summary("id", "Protest Id")] int protestId)
        {
            await DeferAsync(ephemeral: false);

            string? noticeBoardChannelIdStr = Env.GetString("NOTICE_BOARD_CHANNEL_ID");
            if (!ulong.TryParse(noticeBoardChannelIdStr, out ulong noticeBoardId))
            {
                await FollowupAsync("Invalid or missing NOTICE_BOARD_CHANNEL_ID in .env", ephemeral: false);
                return;
            }

            // Load protest from DB
            Protest? protest = await db.Protests.FindAsync(protestId);
            if (protest == null)
            {
                await FollowupAsync($"No protest found with ID {protestId}.", ephemeral: false);
                return;
            }
            if (protest.Published == true)
            {
                await FollowupAsync($"Protest with ID {protestId} has already been published", ephemeral: false);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Penalty Notice")
                .WithDescription(message)
                .WithColor(new Discord.Color(230, 126, 34))
                .WithCurrentTimestamp()
                .Build();

            var confirmationEmbed = new EmbedBuilder()
                .WithTitle("Penalty Published")
                .AddField("Protest ID", protestId)
                .AddField("Destination Channel", teamChannel)
                .WithColor(new Discord.Color(230, 126, 34))
                .WithCurrentTimestamp()
                .Build();

            if (client.GetChannel(noticeBoardId) is not IMessageChannel noticeBoard)
            {
                await FollowupAsync("Could not find the protest channel. Please check the configuration.", ephemeral: false);
                return;
            }

            await teamChannel.SendMessageAsync(text: roleToTag.Mention, embed: embed);

            await noticeBoard.SendMessageAsync(embed: embed);

            protest.Published = true;
            db.Protests.Update(protest);
            await db.SaveChangesAsync();

            await FollowupAsync(embed: confirmationEmbed, ephemeral: false);
        }
    }
}
