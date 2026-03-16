using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StoryForgeBot
{
    class Program
    {
        private static DiscordSocketClient _client = null!;

        static async Task Main(string[] args)
        {
            // Read bot token from environment variable — never hardcode tokens
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("ERROR: DISCORD_TOKEN environment variable not set.");
                return;
            }

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                               | GatewayIntents.GuildMessages
                               | GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);

            _client.Log            += Log;
            _client.Ready          += Ready;
            _client.MessageReceived += HandleMessage;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Keep the bot running forever
            await Task.Delay(Timeout.Infinite);
        }

        // Logs Discord.Net messages to console
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine($"[{msg.Severity}] {msg.Source}: {msg.Message}");
            return Task.CompletedTask;
        }

        // Called when bot connects successfully
        private static async Task Ready()
        {
            Console.WriteLine($"Bot is online as {_client.CurrentUser.Username}");
            await _client.SetGameAsync("StoryForge Launcher", type: ActivityType.Playing);
        }

        // Handles every message the bot can see
        private static async Task HandleMessage(SocketMessage message)
        {
            // Ignore messages from other bots
            if (message.Author.IsBot) return;

            // Only respond to messages starting with !
            if (message is not SocketUserMessage msg) return;
            if (!msg.Content.StartsWith("!")) return;

            var command = msg.Content.Trim().ToLower();

            switch (command)
            {
                case "!info":
                    await msg.Channel.SendMessageAsync(embed: BuildInfoEmbed());
                    break;

                case "!download":
                    await msg.Channel.SendMessageAsync(embed: BuildDownloadEmbed());
                    break;

                case "!season1":
                    await msg.Channel.SendMessageAsync(embed: BuildSeasonEmbed(1));
                    break;

                case "!season2":
                    await msg.Channel.SendMessageAsync(embed: BuildSeasonEmbed(2));
                    break;

                case "!commands":
                case "!help":
                    await msg.Channel.SendMessageAsync(embed: BuildHelpEmbed());
                    break;
            }
        }

        // !info embed
        private static Embed BuildInfoEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("⚒  StoryForge Launcher")
                .WithDescription("A sleek launcher for **Minecraft: Story Mode** Season 1 & 2.\nBuilt in C# + WPF by **B0zin0**.")
                .WithColor(new Color(0xFF, 0xAA, 0x00))
                .AddField("Version",    "v1.0",                  inline: true)
                .AddField("Built with", "C# + WPF (.NET 8)",     inline: true)
                .AddField("Platform",   "Windows 10/11 (64-bit)", inline: true)
                .AddField("Features",
                    "• One-click season launch\n" +
                    "• Video background\n" +
                    "• Mod manager with categories\n" +
                    "• Discord Rich Presence\n" +
                    "• Auto update checker")
                .WithFooter("Not affiliated with Telltale Games or Mojang")
                .WithCurrentTimestamp()
                .Build();
        }

        // !download embed
        private static Embed BuildDownloadEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("⬇  Download StoryForge")
                .WithDescription("Click the link below to download the latest release.")
                .WithColor(new Color(0x55, 0xFF, 0x55))
                .AddField("Latest Release",
                    "[StoryForge v1.0](https://github.com/B0zin0/StoryForge/releases/latest)")
                .AddField("Instructions",
                    "1. Download the `.zip`\n" +
                    "2. Extract — keep `StoryForge.exe` and `Assets/` together\n" +
                    "3. Run `StoryForge.exe`\n" +
                    "4. Go to Settings and set your game paths")
                .WithFooter("Windows 10/11 only — no install required")
                .WithCurrentTimestamp()
                .Build();
        }

        // !season1 / !season2 embed
        private static Embed BuildSeasonEmbed(int season)
        {
            var color  = season == 1
                ? new Color(0xAA, 0x44, 0xFF)
                : new Color(0x00, 0xFF, 0xAA);

            var desc = season == 1
                ? "Follow **Jesse** and friends as they set out on an epic quest to find **The Order of the Stone**."
                : "Jesse faces a new adventure involving a **mysterious portal** and a place called the **Old Builders**.";

            var episodes = season == 1
                ? "8 episodes (5 main + 3 Adventure Pass)"
                : "5 episodes";

            return new EmbedBuilder()
                .WithTitle($"🎮  Minecraft: Story Mode — Season {season}")
                .WithDescription(desc)
                .WithColor(color)
                .AddField("Episodes", episodes,        inline: true)
                .AddField("Developer", "Telltale Games", inline: true)
                .AddField("Year", season == 1 ? "2015" : "2017", inline: true)
                .WithFooter("Use !download to get the StoryForge launcher")
                .WithCurrentTimestamp()
                .Build();
        }

        // !help embed
        private static Embed BuildHelpEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("📋  StoryForge Bot Commands")
                .WithColor(new Color(0xFF, 0xAA, 0x00))
                .AddField("!info",     "Info about the StoryForge launcher")
                .AddField("!download", "Get the download link")
                .AddField("!season1",  "Info about MCSM Season 1")
                .AddField("!season2",  "Info about MCSM Season 2")
                .AddField("!help",     "Show this command list")
                .WithFooter("StoryForge Bot by B0zin0")
                .Build();
        }
    }
}
