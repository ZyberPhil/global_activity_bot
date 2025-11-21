using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GlobalStatsBot.Services;

public class XpMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<XpMessageHandler> _logger;
    private readonly ConcurrentDictionary<ulong, DateTime> _cooldowns = new();
    private readonly TimeSpan _cooldownWindow = TimeSpan.FromSeconds(3);

    public XpMessageHandler(IServiceScopeFactory scopeFactory, ILogger<XpMessageHandler> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnMessageCreatedAsync(DiscordClient client, MessageCreateEventArgs e)
    {
        if (e.Author is null || e.Author.IsBot)
            return;

        if (e.Message?.WebhookId is not null)
            return;

        if (e.Guild is null || e.Channel?.IsPrivate == true)
            return;

        var now = DateTime.UtcNow;

        if (_cooldowns.TryGetValue(e.Author.Id, out var lastGrant) && now - lastGrant < _cooldownWindow)
            return;

        _cooldowns[e.Author.Id] = now;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var guildService = scope.ServiceProvider.GetRequiredService<GuildService>();
            var statsService = scope.ServiceProvider.GetRequiredService<StatsService>();

            var guildEntity = await guildService.GetGuildByDiscordIdAsync(e.Guild.Id);
            if (guildEntity is null)
            {
                guildEntity = await guildService.GetOrCreateGuildAsync(e.Guild.Id, e.Guild.Name, e.Guild.IconUrl);
            }

            if (guildEntity.IsXpEnabled.HasValue && !guildEntity.IsXpEnabled.Value)
                return;

            await statsService.AddXpForMessageAsync(e.Author, e.Guild);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Verarbeiten der Nachricht für XP (Guild {GuildId}, User {UserId}).", e.Guild?.Id, e.Author?.Id);
        }
    }
}
