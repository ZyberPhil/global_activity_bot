namespace GlobalStatsBot.Configuration;

public class DiscordBotOptions
{
    public const string SectionName = "Discord";

    // Hinweis: Token wird ausschließlich aus der .env (DISCORD_BOT_TOKEN) gelesen
    public string Intents { get; set; } = "Guilds, GuildMessages";
}