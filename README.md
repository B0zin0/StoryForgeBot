# StoryForge Bot

Discord bot for the StoryForge MCSM Launcher.

## Commands

| Command | Description |
|---|---|
| `!info` | Info about the StoryForge launcher |
| `!download` | Get the download link |
| `!status` | Live download stats from GitHub |
| `!changelog` | Latest version patch notes |
| `!season1` | Info about MCSM Season 1 |
| `!season2` | Info about MCSM Season 2 |
| `!compare` | Season 1 vs Season 2 side by side |
| `!mods` | Mod info and supported file types |
| `!quote` | Random MCSM quote |
| `!fact` | Random MCSM fun fact |
| `!poll` | Vote Season 1 vs Season 2 |
| `!screenshot` | Share a screenshot (attach image) |
| `!help` | Show all commands |

## Deploying to Railway

1. Push this folder to a GitHub repo
2. Go to [railway.app](https://railway.app) and create a new project
3. Connect your GitHub repo
4. Go to **Variables** and add:
   - `DISCORD_TOKEN` = your bot token from discord.com/developers
   - `ANNOUNCE_CHANNEL_ID` = right click your announcement channel in Discord → Copy Channel ID
5. Railway auto-detects the Dockerfile and deploys

## Getting your bot token

1. Go to [discord.com/developers/applications](https://discord.com/developers/applications)
2. Select your app → **Bot** → **Reset Token** → copy it
3. Paste it as the `DISCORD_TOKEN` variable in Railway

## Inviting the bot to your server

In the Developer Portal go to **OAuth2 → URL Generator**:
- Scopes: `bot`, `applications.commands`
- Permissions: `Send Messages`, `Read Message History`, `View Channels`

Copy the generated URL and open it to invite the bot.
