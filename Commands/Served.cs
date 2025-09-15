using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using RaceControlBot.Data;
using RaceControlBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace RaceControlBot.Commands
{
    public class ServedCommand(DiscordSocketClient client, ApplicationDbContext db)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("served", "Use to notify RC that you served a penalty", runMode: RunMode.Async)]
        public async Task ServedAsync(
            [Summary("id", "Protest Id")] int protestId,
            [Summary("number", "Your car number")] int number,
            [Summary("lap", "The lap where you served the penalty")] int lap
        )
        {
            await DeferAsync(ephemeral: false);
            if (db.Protests == null)
            {
                await FollowupAsync("Why the fuck is there no database");
                return;
            }
            Protest? protest = await db.Protests.AsNoTracking().FirstOrDefaultAsync(p => p.Id == protestId);
            if (protest == null)
            {
                await ModifyOriginalResponseAsync(m => m.Content = $"No protest found with ID {protestId}.");
                return;
            }

            string? servedChannelIdStr = Env.GetString("SERVED_CHANNEL_ID");
            if (!ulong.TryParse(servedChannelIdStr, out ulong servedChannelId))
            {
                await FollowupAsync("Invalid or missing SERVED_CHANNEL_ID in .env", ephemeral: false);
                return;
            }

            if (client.GetChannel(servedChannelId) is not IMessageChannel servedChannel)
            {
                await FollowupAsync("Could not find the served channel. Please check the configuration.", ephemeral: false);
                return;
            }

            Embed embed = BuildServeEmbed(protest, Color.Orange, "Awaiting decision");


            IUserMessage review = await servedChannel.SendMessageAsync(text: "@here", embed: embed);
            MessageComponent components = BuildServeButtons(protest.Id, servedChannelId, review.Id);
            await review.ModifyAsync(m => { m.Embed = embed; m.Components = components; });

            Embed confirmationEmbed = new EmbedBuilder()
                .WithTitle("Notification successfully submitted")
                .WithDescription("Below you can find the information you submitted:")
                .AddField("Protest ID", protestId)
                .AddField("Car number", number)
                .AddField("Lap number", lap)
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .Build();
            await FollowupAsync(embed: confirmationEmbed);
        }

        [ComponentInteraction("serve-ack:*:*:*")]
        public async Task HandleAcknowledgeAsync(string protestIdStr, string reviewChannelIdStr, string reviewMessageIdStr)
        {
            int protestId = int.Parse(protestIdStr);
            ulong reviewChannelId = ulong.Parse(reviewChannelIdStr);
            ulong reviewMessageId = ulong.Parse(reviewMessageIdStr);
            if (db.Protests == null)
            {
                await FollowupAsync("Why the fuck is there no database");
                return;
            }
            Protest? protest = await db.Protests.FirstOrDefaultAsync(p => p.Id == protestId);
            if (protest == null)
            { 
                await RespondAsync("Protest not found.", ephemeral: true); 
                return; 
            }

            IMessageChannel? reviewChannel = Context.Client.GetChannel(reviewChannelId) as IMessageChannel;
            if (reviewChannel == null)
            { 
                await RespondAsync("Review channel missing.", ephemeral: true); 
                return; 
            }

            IUserMessage? reviewMessage = await reviewChannel.GetMessageAsync(reviewMessageId) as IUserMessage;
            if (reviewMessage == null)
            { 
                await RespondAsync("Review message missing.", ephemeral: true); 
                return; 
            }

            // Update DB here (authoritative action)
            protest.Served = true;
            db.Protests.Update(protest);
            await db.SaveChangesAsync();

            // Flip embed to green, keep buttons
            Embed updated = BuildServeEmbed(protest, Color.Green, $"Acknowledged by {Context.User.Username}#{Context.User.Discriminator}");
            MessageComponent components = BuildServeButtons(protest.Id, reviewChannelId, reviewMessageId);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            await (Context.Interaction as SocketMessageComponent).UpdateAsync(m => { m.Embed = updated; m.Components = components; });
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Notify origin channel
            if (protest.ChannelId != 0)
            {
                IMessageChannel? origin = Context.Client.GetChannel(protest.ChannelId) as IMessageChannel;
                if (origin != null)
                {
                    Embed reply = new EmbedBuilder()
                        .WithTitle("Your penalty has been marked as served")
                        .WithDescription($"Your penalty for protest id {protestId} has been marked as served")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .Build();
                    await origin.SendMessageAsync(embed: reply);
                }
            }
        }

        [ComponentInteraction("serve-deny:*:*:*")]
        public async Task HandleDenyAsync(string protestIdStr, string reviewChannelIdStr, string reviewMessageIdStr)
        {
            ModalBuilder modal = new ModalBuilder()
                .WithTitle("Enter denial reason")
                .WithCustomId($"serve-deny-modal:{protestIdStr}:{reviewChannelIdStr}:{reviewMessageIdStr}")
                .AddTextInput("Reason", "reason", TextInputStyle.Paragraph, placeholder: "Explain briefly", required: true, maxLength: 500);
            await RespondWithModalAsync(modal.Build());
        }

        [ModalInteraction("serve-deny-modal:*:*:*")]
        public async Task HandleDenyModalAsync(string protestIdStr, string reviewChannelIdStr, string reviewMessageIdStr, DenyReasonModal modal)
        {
            await DeferAsync();
            int protestId = int.Parse(protestIdStr);
            ulong reviewChannelId = ulong.Parse(reviewChannelIdStr);
            ulong reviewMessageId = ulong.Parse(reviewMessageIdStr);
            if (db.Protests == null)
            {
                await FollowupAsync("Why the fuck is there no database");
                return;
            }
            Protest? protest = await db.Protests.FirstOrDefaultAsync(p => p.Id == protestId);
            if (protest == null)
            { 
                await RespondAsync("Protest not found.", ephemeral: true); 
                return; 
            }

            // Update DB here (authoritative action)
            protest.Served = false;
            db.Protests.Update(protest);
            await db.SaveChangesAsync();

            IMessageChannel? reviewChannel = Context.Client.GetChannel(reviewChannelId) as IMessageChannel;
            if (reviewChannel == null)
            {
                await FollowupAsync("There is a channel id but no channel");
                return;
            }
            IUserMessage? reviewMessage = await reviewChannel.GetMessageAsync(reviewMessageId) as IUserMessage;

            string reason = modal.Reason ?? string.Empty;
            Embed updated = BuildServeEmbed(protest, Color.Red, $"Denied by {Context.User.Username}#{Context.User.Discriminator}\nReason: {reason}");
            MessageComponent components = BuildServeButtons(protest.Id, reviewChannelId, reviewMessageId);
            if (reviewMessage == null)
            {
                await FollowupAsync("Where is the message for this? I lost it");
                return;
            }
            await reviewMessage.ModifyAsync(m => { m.Embed = updated; m.Components = components; });

            if (protest.ChannelId != 0)
            {
                IMessageChannel? origin = Context.Client.GetChannel(protest.ChannelId) as IMessageChannel;
                if (origin != null)
                {
                    Embed reply = new EmbedBuilder()
                        .WithTitle("Your penalty has NOT been marked as served")
                        .WithDescription($"Your penalty for protest id {protestId} has NOT been marked as served")
                        .AddField("Reason", reason)
                        .WithColor(Color.Red)
                        .WithCurrentTimestamp()
                        .Build();
                    await origin.SendMessageAsync(embed: reply);
                }
            }
        }

        private static Embed BuildServeEmbed(Protest p, Color color, string statusLine)
        {
            EmbedBuilder eb = new EmbedBuilder()
                .WithColor(color)
                .WithTitle("Serve Decision")
                .WithDescription($"Protest ID: {p.Id}")
                .AddField("Origin Car", p.CarNumber.ToString(), true)
                .AddField("Penalty", p.Penalty)
                .AddField("Status", statusLine, false)
                .WithCurrentTimestamp();

            return eb.Build();
        }

        private static MessageComponent BuildServeButtons(int protestId, ulong reviewChannelId, ulong reviewMessageId)
        {
            ComponentBuilder cb = new ComponentBuilder();
            cb.WithButton("Acknowledge", customId: $"serve-ack:{protestId}:{reviewChannelId}:{reviewMessageId}", style: ButtonStyle.Success);
            cb.WithButton("Deny", customId: $"serve-deny:{protestId}:{reviewChannelId}:{reviewMessageId}", style: ButtonStyle.Danger);
            return cb.Build();
        }

        // Modal payload
        public class DenyReasonModal : IModal
        {
            public string Title => "Enter denial reason";

            [InputLabel("Reason")]
            [ModalTextInput("reason", TextInputStyle.Paragraph, maxLength: 500, placeholder: "Explain briefly")]
            public string Reason { get; set; } = string.Empty;
        }
    }
}
