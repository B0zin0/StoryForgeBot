# StoryForge Bot

Discord bot for the StoryForge MCSM Launcher.

## Commands

| Command | Description |
|---|---|
| `!info` | Info about the StoryForge launcher |
| `!download` | Get the download link |
| `!season1` | Info about MCSM Season 1 |
| `!season2` | Info about MCSM Season 2 |
| `!help` | Show all commands |

## Deploying to Railway

1. Push this folder to a GitHub repo
2. Go to [railway.app](https://railway.app) and create a new project
3. Connect your GitHub repo
4. Go to **Variables** and add:
   - `DISCORD_TOKEN` = your bot token from discord.com/developers
5. Railway auto-detects the Dockerfile and deploys

## Getting your bot token

1. Go to [discord.com/developers/applications](https://discord.com/developers/applications)
2. Select your app → **Bot** → **Reset Token** → copy it
3. Paste it as the `DISCORD_TOKEN` variable in Railway

## Inviting the bot to your server

In the Developer Portal go to **OAuth2 → URL Generator**:
- Scopes: `bot`
- Permissions: `Send Messages`, `Read Message History`, `View Channels`

Copy the generated URL and open it to invite the bot.
