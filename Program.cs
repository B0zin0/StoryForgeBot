using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StoryForgeBot
{
    class Program
    {
        private static DiscordSocketClient _client = null!;
        private static readonly HttpClient _http   = new();

        // Tracks last known download count for the milestone announcer
        private static long _lastDownloadCount = 0;

        // Milestones to announce (e.g. every 10 downloads, then 50, 100, 500...)
        private static readonly long[] _milestones = { 10, 25, 50, 100, 250, 500, 1000 };

        // Channel ID to post milestone announcements in
        // Replace this with your actual announcements channel ID
        private static ulong _announcementChannelId = 0;

        // Random quotes from MCSM
        private static readonly string[] _quotes =
        {
            "\"You know what they say — keep on keeping on!\" — Lukas",
            "\"The Order of the Stone didn't just save the world. They ARE the world.\" — Gabriel",
            "\"A builder builds. That's what we do.\" — Jesse",
            "\"I'm not a hero. I'm just... a person.\" — Jesse",
            "\"Sometimes the bravest thing you can do is admit you need help.\" — Petra",
            "\"We're the new Order of the Stone. And we're gonna save everyone.\" — Jesse",
            "\"Llamas are majestic creatures.\" — Reuben",
            "\"You can't just punch your way out of everything, Jesse.\" — Olivia",
            "\"I didn't come this far to give up now.\" — Axel",
            "\"The world is what we make of it.\" — Ivor",
            "\"Every ending is just a new beginning.\" — Harper",
            "\"We built this. We can rebuild it.\" — Jesse",
        };

        // Fun facts about MCSM
        private static readonly string[] _facts =
        {
            "Minecraft: Story Mode was developed by Telltale Games and released in October 2015.",
            "The game features Jesse, who can be played as either male or female.",
            "Reuben the pig is one of the most beloved characters in the game.",
            "Season 1 has 8 episodes total — 5 main episodes and 3 Adventure Pass episodes.",
            "Season 2 was released in 2017 and features an entirely new story arc.",
            "The Order of the Stone — Gabriel, Ellegaard, Magnus, and Soren — are named after Minecraft community figures.",
            "The game was delisted from all digital storefronts in June 2019 after Telltale shut down.",
            "StoryForge is one of the only modern launchers keeping MCSM alive for fans.",
            "The Wither Storm boss in Season 1 is one of the most iconic Telltale villains ever created.",
            "Cassie Rose and the White Pumpkin are fan-favorite characters from the Adventure Pass.",
            "MCSM uses the Telltale Tool engine, the same engine used for The Walking Dead and Wolf Among Us.",
            "The game sold over 5 million copies before it was delisted.",
        };

        private static readonly Random _rng = new();

        static async Task Main(string[] args)
        {
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("ERROR: DISCORD_TOKEN not set.");
                return;
            }

            // Optionally read announcement channel from env
            var chanEnv = Environment.GetEnvironmentVariable("ANNOUNCE_CHANNEL_ID");
            if (ulong.TryParse(chanEnv, out var chanId))
                _announcementChannelId = chanId;

            _http.DefaultRequestHeaders.UserAgent.ParseAdd("StoryForgeBot/1.0");

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                               | GatewayIntents.GuildMessages
                               | GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);
            _client.Log             += Log;
            _client.Ready           += Ready;
            _client.MessageReceived += HandleMessage;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Start the download milestone watcher in the background
            _ = Task.Run(DownloadWatcher);

            await Task.Delay(Timeout.Infinite);
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine($"[{msg.Severity}] {msg.Source}: {msg.Message}");
            return Task.CompletedTask;
        }

        private static async Task Ready()
        {
            Console.WriteLine($"Online as {_client.CurrentUser.Username}");
            await _client.SetGameAsync("StoryForge Launcher", type: ActivityType.Playing);
        }

        // ── Command router ───────────────────────────────────────────────
        private static async Task HandleMessage(SocketMessage message)
        {
            if (message.Author.IsBot) return;
            if (message is not SocketUserMessage msg) return;
            if (!msg.Content.StartsWith("!")) return;

            // Split into command + optional args
            var parts   = msg.Content.Trim().Split(' ', 2);
            var command = parts[0].ToLower();

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

                case "!compare":
                    await msg.Channel.SendMessageAsync(embed: BuildCompareEmbed());
                    break;

                case "!status":
                    var statusEmbed = await BuildStatusEmbed();
                    await msg.Channel.SendMessageAsync(embed: statusEmbed);
                    break;

                case "!changelog":
                    var changelogEmbed = await BuildChangelogEmbed();
                    await msg.Channel.SendMessageAsync(embed: changelogEmbed);
                    break;

                case "!mods":
                    await msg.Channel.SendMessageAsync(embed: BuildModsEmbed());
                    break;

                case "!quote":
                    await msg.Channel.SendMessageAsync(embed: BuildQuoteEmbed());
                    break;

                case "!fact":
                    await msg.Channel.SendMessageAsync(embed: BuildFactEmbed());
                    break;

                case "!poll":
                    await CreatePoll(msg);
                    break;

                case "!screenshot":
                    await HandleScreenshot(msg);
                    break;

                case "!commands":
                case "!help":
                    await msg.Channel.SendMessageAsync(embed: BuildHelpEmbed());
                    break;
            }
        }

        // ── Embeds ───────────────────────────────────────────────────────

        private static Embed BuildInfoEmbed() =>
            new EmbedBuilder()
                .WithTitle("⚒  StoryForge Launcher")
                .WithDescription("A sleek launcher for **Minecraft: Story Mode** Season 1 & 2.\nBuilt in C# + WPF by **B0zin0**.")
                .WithColor(new Color(0xFF, 0xAA, 0x00))
                .AddField("Version",    "v1.0",                   inline: true)
                .AddField("Built with", "C# + WPF (.NET 8)",      inline: true)
                .AddField("Platform",   "Windows 10/11 (64-bit)", inline: true)
                .AddField("Features",
                    "• One-click season launch\n" +
                    "• Video background & Minecraft font\n" +
                    "• Mod manager with categories\n" +
                    "• Discord Rich Presence\n" +
                    "• Auto update checker\n" +
                    "• Volume slider & window presets")
                .WithFooter("Not affiliated with Telltale Games or Mojang")
                .WithCurrentTimestamp()
                .Build();

        private static Embed BuildDownloadEmbed() =>
            new EmbedBuilder()
                .WithTitle("⬇  Download StoryForge")
                .WithDescription("Get the latest release below.")
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

        private static Embed BuildSeasonEmbed(int season)
        {
            var color = season == 1
                ? new Color(0xAA, 0x44, 0xFF)
                : new Color(0x00, 0xFF, 0xAA);

            var desc = season == 1
                ? "Follow **Jesse** and friends on an epic quest to find **The Order of the Stone** and defeat the terrifying **Wither Storm**."
                : "Jesse faces a new adventure involving a **mysterious portal** and a place called the **Old Builders' world**.";

            var episodes = season == 1
                ? "8 episodes (5 main + 3 Adventure Pass)"
                : "5 episodes";

            var characters = season == 1
                ? "Jesse, Petra, Axel, Olivia, Lukas, Gabriel, Magnus, Ellegaard, Soren, Ivor"
                : "Jesse, Petra, Lukas, Radar, Jack, Nurm, Harper, Stella";

            return new EmbedBuilder()
                .WithTitle($"🎮  Minecraft: Story Mode — Season {season}")
                .WithDescription(desc)
                .WithColor(color)
                .AddField("Episodes",   episodes,                            inline: true)
                .AddField("Developer",  "Telltale Games",                    inline: true)
                .AddField("Year",       season == 1 ? "2015" : "2017",       inline: true)
                .AddField("Key Characters", characters)
                .WithFooter("Use !download to get the StoryForge launcher")
                .WithCurrentTimestamp()
                .Build();
        }

        private static Embed BuildCompareEmbed() =>
            new EmbedBuilder()
                .WithTitle("⚔  Season 1 vs Season 2")
                .WithColor(new Color(0xFF, 0xAA, 0x00))
                .AddField("📅 Release Year",    "S1: 2015  |  S2: 2017",          inline: false)
                .AddField("📺 Episodes",        "S1: 8 episodes  |  S2: 5 episodes", inline: false)
                .AddField("👾 Main Villain",    "S1: Wither Storm  |  S2: The Admin", inline: false)
                .AddField("🌍 Setting",         "S1: Overworld  |  S2: Sky dimension & Admin's world", inline: false)
                .AddField("🎭 Tone",            "S1: Lighthearted adventure  |  S2: Darker, higher stakes", inline: false)
                .AddField("⭐ Fan Favourite",   "Most fans prefer Season 1 for its story and characters")
                .WithFooter("Both seasons supported by StoryForge!")
                .WithCurrentTimestamp()
                .Build();

        private static async Task<Embed> BuildStatusEmbed()
        {
            long downloads = 0;
            string latestVersion = "unknown";

            try
            {
                var json = await _http.GetStringAsync(
                    "https://api.github.com/repos/B0zin0/StoryForge/releases");
                using var doc = JsonDocument.Parse(json);

                foreach (var release in doc.RootElement.EnumerateArray())
                {
                    if (latestVersion == "unknown")
                        latestVersion = release.GetProperty("tag_name").GetString() ?? "unknown";

                    if (release.TryGetProperty("assets", out var assets))
                        foreach (var asset in assets.EnumerateArray())
                            downloads += asset.GetProperty("download_count").GetInt64();
                }
            }
            catch { }

            return new EmbedBuilder()
                .WithTitle("📊  StoryForge Status")
                .WithColor(new Color(0x00, 0xCC, 0xFF))
                .AddField("Latest Version", latestVersion,          inline: true)
                .AddField("Total Downloads", $"{downloads:N0}",     inline: true)
                .AddField("Platform",        "Windows 10/11",       inline: true)
                .AddField("GitHub", "[B0zin0/StoryForge](https://github.com/B0zin0/StoryForge)")
                .WithFooter("Stats pulled live from GitHub")
                .WithCurrentTimestamp()
                .Build();
        }

        private static async Task<Embed> BuildChangelogEmbed()
        {
            string version     = "unknown";
            string body        = "No changelog available.";
            string releaseUrl  = "https://github.com/B0zin0/StoryForge/releases";

            try
            {
                var json = await _http.GetStringAsync(
                    "https://api.github.com/repos/B0zin0/StoryForge/releases/latest");
                using var doc = JsonDocument.Parse(json);

                version    = doc.RootElement.GetProperty("tag_name").GetString() ?? "unknown";
                releaseUrl = doc.RootElement.GetProperty("html_url").GetString()  ?? releaseUrl;
                body       = doc.RootElement.GetProperty("body").GetString()       ?? body;

                // Trim if too long for Discord embed
                if (body.Length > 1000) body = body[..1000] + "...";
            }
            catch { }

            return new EmbedBuilder()
                .WithTitle($"📋  StoryForge {version} — Changelog")
                .WithDescription(body)
                .WithUrl(releaseUrl)
                .WithColor(new Color(0xFF, 0xAA, 0x00))
                .WithFooter("Full release notes on GitHub")
                .WithCurrentTimestamp()
                .Build();
        }

        private static Embed BuildModsEmbed() =>
            new EmbedBuilder()
                .WithTitle("🧩  Community Mods")
                .WithDescription("Mods compatible with the StoryForge launcher.\nInstall via **Mod Manager → + Install Mod**.")
                .WithColor(new Color(0xAA, 0x44, 0xFF))
                .AddField("Supported File Types",
                    "`.d3dtx` Textures  •  `.ttarch2` Archives\n" +
                    "`.lua` Scripts  •  `.bank` Audio\n" +
                    "`.landb` `.pak` `.zip` Packages")
                .AddField("Find Mods",
                    "• Search GitHub for `MCSM mods`\n" +
                    "• Check Nexus Mods for Minecraft Story Mode\n" +
                    "• Ask in this server!")
                .WithFooter("Mods are not affiliated with Telltale or Mojang")
                .WithCurrentTimestamp()
                .Build();

        private static Embed BuildQuoteEmbed()
        {
            var quote = _quotes[_rng.Next(_quotes.Length)];
            return new EmbedBuilder()
                .WithTitle("💬  MCSM Quote")
                .WithDescription(quote)
                .WithColor(new Color(0x55, 0xFF, 0x55))
                .WithFooter("Use !quote again for another one")
                .WithCurrentTimestamp()
                .Build();
        }

        private static Embed BuildFactEmbed()
        {
            var fact = _facts[_rng.Next(_facts.Length)];
            return new EmbedBuilder()
                .WithTitle("💡  MCSM Fun Fact")
                .WithDescription(fact)
                .WithColor(new Color(0x00, 0xCC, 0xFF))
                .WithFooter("Use !fact again for another one")
                .WithCurrentTimestamp()
                .Build();
        }

        private static async Task CreatePoll(SocketUserMessage msg)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🗳  Season Poll")
                .WithDescription("Which season of Minecraft: Story Mode is your favourite?")
                .WithColor(new Color(0xFF, 0xAA, 0x00))
                .AddField("1️⃣  Season 1", "The Wither Storm saga — 2015", inline: true)
                .AddField("2️⃣  Season 2", "The Admin saga — 2017",        inline: true)
                .WithFooter("React below to vote!")
                .WithCurrentTimestamp()
                .Build();

            var pollMsg = await msg.Channel.SendMessageAsync(embed: embed);

            // Add reactions for voting
            await pollMsg.AddReactionAsync(new Emoji("1️⃣"));
            await pollMsg.AddReactionAsync(new Emoji("2️⃣"));
        }

        private static async Task HandleScreenshot(SocketUserMessage msg)
        {
            // Check if an image was attached
            if (msg.Attachments.Count == 0)
            {
                await msg.Channel.SendMessageAsync(
                    "📸 Attach your screenshot to the `!screenshot` command and I'll share it!");
                return;
            }

            var attachment = msg.Attachments.First();
            var embed = new EmbedBuilder()
                .WithTitle("📸  Community Screenshot")
                .WithDescription($"Shared by **{msg.Author.Username}**")
                .WithImageUrl(attachment.Url)
                .WithColor(new Color(0xAA, 0x44, 0xFF))
                .WithFooter("Share yours with !screenshot")
                .WithCurrentTimestamp()
                .Build();

            await msg.Channel.SendMessageAsync(embed: embed);
        }

        private static Embed BuildHelpEmbed() =>
            new EmbedBuilder()
                .WithTitle("📋  StoryForge Bot Commands")
                .WithColor(new Color(0xFF, 0xAA, 0x00))
                .AddField("🚀 Launcher",
                    "`!info` — Launcher info\n" +
                    "`!download` — Get the download link\n" +
                    "`!status` — Live download stats from GitHub\n" +
                    "`!changelog` — Latest version patch notes")
                .AddField("🎮 Game",
                    "`!season1` — Info about Season 1\n" +
                    "`!season2` — Info about Season 2\n" +
                    "`!compare` — S1 vs S2 side by side\n" +
                    "`!mods` — Mod info & file types")
                .AddField("🎉 Fun",
                    "`!quote` — Random MCSM quote\n" +
                    "`!fact` — Random MCSM fun fact\n" +
                    "`!poll` — Vote S1 vs S2\n" +
                    "`!screenshot` — Share a screenshot (attach image)")
                .WithFooter("StoryForge Bot by B0zin0")
                .Build();

        // ── Download milestone watcher ───────────────────────────────────
        // Runs in background, checks GitHub every 10 minutes
        // Posts in announcement channel when a milestone is hit
        private static async Task DownloadWatcher()
        {
            // Wait for client to be ready
            await Task.Delay(5000);

            while (true)
            {
                try
                {
                    long total = 0;
                    var json = await _http.GetStringAsync(
                        "https://api.github.com/repos/B0zin0/StoryForge/releases");
                    using var doc = JsonDocument.Parse(json);

                    foreach (var release in doc.RootElement.EnumerateArray())
                        if (release.TryGetProperty("assets", out var assets))
                            foreach (var asset in assets.EnumerateArray())
                                total += asset.GetProperty("download_count").GetInt64();

                    // Check if we just crossed a milestone
                    foreach (var milestone in _milestones)
                    {
                        if (_lastDownloadCount < milestone && total >= milestone)
                        {
                            await AnnounceDownloadMilestone(milestone);
                            break;
                        }
                    }

                    _lastDownloadCount = total;
                }
                catch { /* Network error — try again next tick */ }

                // Check every 10 minutes
                await Task.Delay(TimeSpan.FromMinutes(10));
            }
        }

        private static async Task AnnounceDownloadMilestone(long count)
        {
            if (_announcementChannelId == 0) return;
            if (_client.GetChannel(_announcementChannelId) is not IMessageChannel channel) return;

            var embed = new EmbedBuilder()
                .WithTitle("🎉  Download Milestone!")
                .WithDescription($"**StoryForge** just hit **{count:N0} downloads!**\nThank you to everyone keeping MCSM alive! ❤")
                .WithColor(new Color(0xFF, 0xAA, 0x00))
                .AddField("Download it here",
                    "[StoryForge Latest Release](https://github.com/B0zin0/StoryForge/releases/latest)")
                .WithCurrentTimestamp()
                .Build();

            await channel.SendMessageAsync(embed: embed);
        }
    }
}
