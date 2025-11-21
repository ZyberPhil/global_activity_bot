using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GlobalStatsBot.Dtos;
using GlobalStatsBot.Services;
using Microsoft.Extensions.Logging;

namespace GlobalStatsBot.Commands;

[SlashCommandGroup("top", "Zeigt Leaderboards für XP und Nachrichten an.")]
public sealed class TopCommands : ApplicationCommandModule
{
    private readonly StatsService _statsService;
    private readonly ILogger<TopCommands> _logger;

    public TopCommands(StatsService statsService, ILogger<TopCommands> logger)
    {
        _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [SlashCommand("global", "Zeigt das globale Leaderboard über alle Server.")]
    public async Task ShowGlobalAsync(
        InteractionContext ctx,
        [Option("limit", "Wie viele Einträge sollen angezeigt werden? (1-25)")] long limit = 10)
    {
        var take = NormalizeLimit(limit);

        try
        {
            var entries = await _statsService.GetTopGlobalUsersAsync(take);
            if (entries.Count == 0)
            {
                await RespondEphemeralAsync(ctx, "Es wurden noch keine globalen XP gesammelt.");
                return;
            }

            var embed = BuildLeaderboardEmbed("Globales Leaderboard", entries);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim globalen Leaderboard (User {UserId}).", ctx.User.Id);
            await RespondEphemeralAsync(ctx, "Beim Laden des Leaderboards ist ein Fehler aufgetreten.");
        }
    }

    [SlashCommand("guild", "Zeigt das Leaderboard für diesen Server.")]
    public async Task ShowGuildAsync(
        InteractionContext ctx,
        [Option("limit", "Wie viele Einträge sollen angezeigt werden? (1-25)")] long limit = 10)
    {
        if (ctx.Guild is null)
        {
            await RespondEphemeralAsync(ctx, "Dieser Command kann nur in Servern genutzt werden.");
            return;
        }

        var take = NormalizeLimit(limit);

        try
        {
            var entries = await _statsService.GetTopUsersByGuildAsync(ctx.Guild.Id, take);
            if (entries.Count == 0)
            {
                await RespondEphemeralAsync(ctx, "Auf diesem Server wurden noch keine XP gesammelt.");
                return;
            }

            var title = $"Top {entries.Count} – {ctx.Guild.Name}";
            var embed = BuildLeaderboardEmbed(title, entries, ctx.Guild.Name);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Guild-Leaderboard für Guild {GuildId} durch {UserId}.", ctx.Guild.Id, ctx.User.Id);
            await RespondEphemeralAsync(ctx, "Beim Laden des Leaderboards ist ein Fehler aufgetreten.");
        }
    }

    [SlashCommand("channel", "Zeigt das Leaderboard für einen Channel dieses Servers.")]
    public async Task ShowChannelAsync(
        InteractionContext ctx,
        [Option("channel", "Der Channel, dessen Ranking angezeigt werden soll")] DiscordChannel channel,
        [Option("limit", "Wie viele Einträge sollen angezeigt werden? (1-25)")] long limit = 10)
    {
        if (ctx.Guild is null)
        {
            await RespondEphemeralAsync(ctx, "Dieser Command kann nur in Servern genutzt werden.");
            return;
        }

        if (channel is null || channel.GuildId != ctx.Guild.Id)
        {
            await RespondEphemeralAsync(ctx, "Bitte wähle einen Channel aus diesem Server aus.");
            return;
        }

        var take = NormalizeLimit(limit);

        try
        {
            var entries = await _statsService.GetTopUsersByChannelAsync(ctx.Guild.Id, channel.Id, take);
            if (entries.Count == 0)
            {
                await RespondEphemeralAsync(ctx, "In diesem Channel wurden noch keine XP vergeben.");
                return;
            }

            var title = $"Top {entries.Count} – {channel.Name}";
            var embed = BuildLeaderboardEmbed(title, entries, channel.Mention);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Channel-Leaderboard für Guild {GuildId} / Channel {ChannelId}.", ctx.Guild.Id, channel.Id);
            await RespondEphemeralAsync(ctx, "Beim Laden des Leaderboards ist ein Fehler aufgetreten.");
        }
    }

    private static DiscordEmbed BuildLeaderboardEmbed(string title, IReadOnlyList<LeaderboardEntryDto> entries, string? footer = null)
    {
        var description = string.Join('\n', entries.Select((entry, index) => FormatLeaderboardLine(index, entry)));

        var builder = new DiscordEmbedBuilder()
            .WithTitle(title)
            .WithColor(DiscordColor.Blurple)
            .WithDescription(description);

        if (!string.IsNullOrWhiteSpace(footer))
            builder.WithFooter(footer);

        return builder.Build();
    }

    private static string FormatLeaderboardLine(int index, LeaderboardEntryDto entry)
    {
        var rank = index + 1;
        var username = string.IsNullOrWhiteSpace(entry.Username)
            ? $"User {entry.DiscordUserId}"
            : entry.Username;

        var xpText = entry.Xp.ToString("N0", CultureInfo.InvariantCulture);
        var messageText = entry.Messages > 0
            ? $" · {entry.Messages.ToString("N0", CultureInfo.InvariantCulture)} Nachrichten"
            : string.Empty;

        return $"{rank}. {username} – {xpText} XP{messageText}";
    }

    private static int NormalizeLimit(long limit)
    {
        if (limit < 1)
            return 1;
        if (limit > 25)
            return 25;
        return (int)limit;
    }

    private static Task RespondEphemeralAsync(InteractionContext ctx, string message)
    {
        var response = new DiscordInteractionResponseBuilder()
            .WithContent(message)
            .AsEphemeral(true);

        return ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
    }
}
