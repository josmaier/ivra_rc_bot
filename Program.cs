using ATVO.RaceControl.Client;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceControlBot.Data;
using RaceControlBot.Models;
using System.Reflection;

namespace RaceControlBot
{
    internal class Program
    {
        private DiscordSocketClient? _client;
        private InteractionService? _commands;
        private IServiceProvider? _services;
        private RaceControlClient? _atvoRaceControlClient;
        public static ulong[]? rcOnlyCommandRoleList;
        public static string? sheetUrl;
        public static bool IVRA;
        public static bool ATVO_RC;


        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {

            string? envPath = Environment.GetEnvironmentVariable("DOTNET_ENV_PATH");

            if (string.IsNullOrEmpty(envPath))
            {
                envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            }

            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                Console.WriteLine($".env loaded from: {envPath}");
            }
            else
            {
                Console.WriteLine($"No .env file found at {envPath}");
            }

            string? connectionString = Env.GetString("SQLITE_CONNECTION_STRING") ?? "DataSource=./app.db";
            string? discordToken = Env.GetString("DISCORD_TOKEN");
            string? guildIdStr = Env.GetString("GUILD_ID");
            string? rcRoleId = Env.GetString("RACE_CONTROL_ROLE_ID");
            string? adminRoleId = Env.GetString("ADMIN_ROLE_ID");
            IVRA = Env.GetBool("IVRA");
            ATVO_RC = Env.GetBool("ATVO");
            rcOnlyCommandRoleList = HelperFunctions.RoleCheck.ParseRoleIds($"{rcRoleId},{adminRoleId}");

            Console.WriteLine($"IVRA: {IVRA.ToString()} \n ATVO_RC: {ATVO_RC.ToString()}");
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
                .AddSingleton<RaceControlClient>(RaceControlClient.Instance)
                .BuildServiceProvider();

            this._services = services;
            this._client = services.GetRequiredService<DiscordSocketClient>();
            this._commands = services.GetRequiredService<InteractionService>();
            this._atvoRaceControlClient = services.GetRequiredService<RaceControlClient>();

            if (ATVO_RC)
            {
                ConnectionResult result = await this._atvoRaceControlClient.Start("127.0.0.1", 1337, "test", true);
                Console.WriteLine(result.Message);
            }


            this._client.Log += LogAsync;
            this._commands.Log += LogAsync;

            using (IServiceScope scope = services.CreateScope()) //to not open the context for longer than we need we need a one time scope
            {
                ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                AppSetting? setting = await db.Settings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == "SheetUrl");

                Program.sheetUrl = setting?.Value ?? string.Empty;
            }

            await this._commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            await this._client.LoginAsync(TokenType.Bot, discordToken);
            await this._client.StartAsync();

            this._client.Ready += async () =>
            {
                await this._commands.RegisterCommandsToGuildAsync(guildId);
                Log("Main", "Slash commands registered.");
                string? channelIdStr = Env.GetString("RESTART_CHANNEL_ID");
                if (!ulong.TryParse(channelIdStr, out ulong restartChannelId))
                {
                    Console.WriteLine("No restart channel ID set");
                    return;
                }

                ITextChannel? restartChannel = await _client.GetChannelAsync(restartChannelId) as ITextChannel;
                if (restartChannel == null)
                {
                    Console.WriteLine("Restart channel does not exist");
                    return;
                }

                string changelog =
                    @"Started successfully!

                    Changelog:
                    1.0  Initial Release, rewrite in C# with DB context
                    1.1  Added toggles for the IVRA mode and ATVO RC";

                await restartChannel.SendMessageAsync(changelog);
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

        private static void Log(string source, string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"{time,-8} {source,-10} {message}");
        }

    }
}
