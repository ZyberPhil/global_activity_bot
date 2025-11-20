using DSharpPlus;
using DSharpPlus.SlashCommands;
using GlobalStatsBot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using GlobalStatsBot.Commands;

namespace GlobalStatsBot.Services;

public class BotService : IHostedService
{
    private readonly ILogger<BotService> _logger;
    private readonly DiscordBotOptions _options;

    private DiscordClient? _client;

    public BotService(
        ILogger<BotService> logger,
        IOptions<DiscordBotOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Discord bot…");

        var token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("No Discord bot token configured. Set DISCORD_BOT_TOKEN in .env or environment variables.");
            return;
        }

        var intents = DiscordIntents.Guilds |
                      DiscordIntents.GuildMessages |
                      DiscordIntents.MessageContents;

        _client = new DiscordClient(new DiscordConfiguration
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = intents,
            AutoReconnect = true,
            MinimumLogLevel = LogLevel.Information
        });

        // SlashCommands registrieren
        var slash = _client.UseSlashCommands();

        // Global registrieren (für Prod) – dauert ca. 1 Stunde bei globalen Commands
        slash.RegisterCommands<PingCommandModule>();

        // Für Debugging schneller: nur auf einem Test-Guild registrieren:
        // var testGuildId = 123456789012345678UL;
        // slash.RegisterCommands<PingCommandModule>(testGuildId);

        _client.Ready += (_, _) =>
        {
            _logger.LogInformation("Discord client is ready and logged in as {Username}#{Discriminator}",
                _client.CurrentUser.Username,
                _client.CurrentUser.Discriminator);
            return Task.CompletedTask;
        };

        await _client.ConnectAsync();

        _logger.LogInformation("Discord bot connected.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            _logger.LogInformation("Stopping Discord bot…");
            await _client.DisconnectAsync();
            _client.Dispose();
        }
    }

    private string GetToken()
    {
        // Nur aus Environment Variable (geladen via .env)
        var envToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        return string.IsNullOrWhiteSpace(envToken) ? string.Empty : envToken;
    }
}