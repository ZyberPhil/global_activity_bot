using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GlobalStatsBot.Services;
using Microsoft.Extensions.Logging;

namespace GlobalStatsBot.Commands;

[SlashCommandGroup("admin", "Administrative Werkzeuge für den Bot.")]
public sealed class AdminCommands : ApplicationCommandModule
{
    private readonly StatsService _statsService;
    private readonly ILogger<AdminCommands> _logger;

    public AdminCommands(StatsService statsService, ILogger<AdminCommands> logger)
    {
        _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [SlashCommand("syncglobalxp", "Synchronisiert den GlobalXpCache aller Nutzer mit den UserStats.")]
    public async Task SyncGlobalXpAsync(InteractionContext ctx)
    {
        if (ctx.Guild is null || ctx.Member is null)
        {
            await RespondEphemeralAsync(ctx, "Dieser Command kann nur auf einem Server verwendet werden.");
            return;
        }

        if (!ctx.Member.Permissions.HasPermission(Permissions.ManageGuild))
        {
            await RespondEphemeralAsync(ctx, "Du benötigst ManageGuild, um diesen Command auszuführen.");
            return;
        }

        await ctx.DeferAsync(true);

        try
        {
            var affected = await _statsService.SynchronizeGlobalXpCacheAsync();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"GlobalXpCache wurde für {affected:N0} Nutzer synchronisiert."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Synchronisieren des GlobalXpCache ausgelöst von {UserId}.", ctx.User.Id);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Beim Synchronisieren ist ein Fehler aufgetreten."));
        }
    }

    private static Task RespondEphemeralAsync(InteractionContext ctx, string message)
    {
        var response = new DiscordInteractionResponseBuilder()
            .WithContent(message)
            .AsEphemeral(true);

        return ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
    }
}
