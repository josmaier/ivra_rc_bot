using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceControlBot.Data;
using RaceControlBot.Models;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RaceControlBot
{
    internal class Program
    {
        private DiscordSocketClient? _client;
        private InteractionService? _commands;
        private IServiceProvider? _services;
        public static ulong[]? rcOnlyCommandRoleList;
        public static string? sheetURL;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {

            // Load environment variables from .env file
            Env.Load();

            string? connectionString = Env.GetString("SQLITE_CONNECTION_STRING");
            string? discordToken = Env.GetString("DISCORD_TOKEN");
            string? guildIdStr = Env.GetString("GUILD_ID");
            string? rcRoleID = Env.GetString("RACE_CONTROL_ROLE_ID");
            string? adminRoleID = Env.GetString("ADMIN_ROLE_ID");
            rcOnlyCommandRoleList = HelperFunctions.RoleCheck.ParseRoleIds($"{rcRoleID},{adminRoleID}");

            if (string.IsNullOrWhiteSpace(discordToken))
            {
                Console.WriteLine("DISCORD_TOKEN not set in .env");
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine("SQLITE_CONNECTION_STRING not set in .env");
                return;
            }

            if (string.IsNullOrWhiteSpace(guildIdStr) || !ulong.TryParse(guildIdStr, out ulong guildId))
            {
                Console.WriteLine("GUILD_ID is missing or invalid in .env");
                return;
            }
            ServiceProvider services = new ServiceCollection()
                .AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(connectionString))
                .AddSingleton<DiscordSocketClient>(provider =>
                    new DiscordSocketClient(new DiscordSocketConfig
                    {
                        UseInteractionSnowflakeDate = false,
                        GatewayIntents = GatewayIntents.Guilds
                    }))
                .AddSingleton<InteractionService>(provider =>
                {
                    DiscordSocketClient client = provider.GetRequiredService<DiscordSocketClient>();
                    return new InteractionService(client.Rest); // or just 'client' depending on your version
                })
                .BuildServiceProvider();

            this._services = services;
            this._client = services.GetRequiredService<DiscordSocketClient>();
            this._commands = services.GetRequiredService<InteractionService>();

            this._client.Log += LogAsync;
            this._commands.Log += LogAsync;

            using (IServiceScope scope = services.CreateScope()) //to not open the context for longer than we need we need a one time scope
            {
                ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                AppSetting? setting = await db.Settings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == "SheetUrl");

                Program.sheetURL = setting?.Value ?? string.Empty;
            }

            await this._commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            await this._client.LoginAsync(TokenType.Bot, discordToken);
            await this._client.StartAsync();

            this._client.Ready += async () =>
            {
                await this._commands.RegisterCommandsToGuildAsync(guildId);
                Console.WriteLine("Slash commands registered.");
            };

            this._client.InteractionCreated += HandleInteractionAsync;


            await Task.Delay(-1);
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                SocketInteractionContext ctx = new SocketInteractionContext(this._client, interaction);
                InteractionService? interactionService = this._commands;
                if (interactionService != null)
                {
                    await interactionService.ExecuteCommandAsync(ctx, this._services);
                    interactionService.SlashCommandExecuted += async (info, ctx, result) =>
                    {
                        if (!result.IsSuccess)
                            await ctx.Interaction.FollowupAsync($"Blocked: {result.ErrorReason}", ephemeral: true);
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }

        private static Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

    }
}
