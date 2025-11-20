using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GlobalStatsBot.Models;
using GlobalStatsBot.Services;
using Microsoft.Extensions.Logging;

namespace GlobalStatsBot.Commands;

[SlashCommandGroup("badge", "Verwaltung globaler Badges.")]
public class BadgeCommands : ApplicationCommandModule
{
    private readonly BadgeService _badgeService;
    private readonly ILogger<BadgeCommands> _logger;

    public BadgeCommands(BadgeService badgeService, ILogger<BadgeCommands> logger)
    {
        _badgeService = badgeService ?? throw new ArgumentNullException(nameof(badgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [SlashCommand("give", "Verleiht einem Nutzer ein globales Badge.")]
    public async Task GiveBadgeAsync(
        InteractionContext ctx,
        [Option("user", "Nutzer, der das Badge erhalten soll")] DiscordUser targetUser,
        [Option("badge_key", "Key des Badges")] string badgeKey,
        [Option("reason", "Optional: Grund für das Badge")] string? reason = null)
    {
        if (ctx.Guild is null || ctx.Member is null)
        {
            await RespondEphemeralAsync(ctx, "Dieser Command kann nur in Servern verwendet werden.");
            return;
        }

        if (!ctx.Member.Permissions.HasPermission(Permissions.ManageGuild))
        {
            await RespondEphemeralAsync(ctx, "Du hast keine Berechtigung, Badges zu vergeben.");
            return;
        }

        try
        {
            var badge = await _badgeService.GetBadgeByKeyAsync(badgeKey);
            if (badge is null)
            {
                await RespondEphemeralAsync(ctx, $"Das Badge mit dem Key `{badgeKey}` existiert nicht.");
                return;
            }

            var userBadges = await _badgeService.GetBadgesForUserAsync(targetUser.Id);
            var alreadyHasBadge = userBadges.Any(b => string.Equals(b.Badge.Key, badge.Key, StringComparison.OrdinalIgnoreCase));

            if (alreadyHasBadge)
            {
                await RespondEphemeralAsync(ctx, $"{targetUser.Mention} besitzt das Badge **{badge.Name}** bereits.");
                return;
            }

            await _badgeService.GiveBadgeToUserAsync(
                targetUser.Id,
                targetUser.Username,
                badge.Key,
                ctx.User.Id,
                reason);

            var builder = new DiscordInteractionResponseBuilder()
                .WithContent($"Badge **{badge.Name}** wurde an {targetUser.Mention} vergeben.");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Vergeben eines Badges durch {InvokerId}.", ctx.User.Id);
            await RespondEphemeralAsync(ctx, "Beim Vergeben des Badges ist ein Fehler aufgetreten. Bitte versuche es erneut.");
        }
    }

    [SlashCommand("list", "Zeigt Badges eines Users.")]
    public async Task ListBadgesAsync(
        InteractionContext ctx,
        [Option("user", "User, dessen Badges angezeigt werden sollen")] DiscordUser? user = null)
    {
        var targetUser = user ?? ctx.User;

        try
        {
            var badges = await _badgeService.GetBadgesForUserAsync(targetUser.Id);
            if (badges.Count == 0)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{targetUser.Mention} hat aktuell keine Badges.")
                        .AsEphemeral(user is not null && targetUser.Id != ctx.User.Id));
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Badges von {targetUser.Username}")
                .WithThumbnail(targetUser.AvatarUrl ?? targetUser.DefaultAvatarUrl)
                .WithColor(DiscordColor.Gold)
                .AddField("Badges", string.Join("\n", badges.Select(entry => FormatBadgeLine(entry))), false)
                .Build();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Auflisten der Badges für {TargetUserId}.", targetUser.Id);
            await RespondEphemeralAsync(ctx, "Beim Abrufen der Badges ist ein Fehler aufgetreten.");
        }
    }

    [SlashCommand("listall", "Zeigt alle verfügbaren Badges (nur Admins).")]
    public async Task ListAllBadgesAsync(InteractionContext ctx)
    {
        if (ctx.Member is null || !ctx.Member.Permissions.HasPermission(Permissions.ManageGuild))
        {
            await RespondEphemeralAsync(ctx, "Du benötigst ManageGuild, um alle Badges zu sehen.");
            return;
        }

        try
        {
            var badges = await _badgeService.GetAllBadgesAsync();
            if (badges.Count == 0)
            {
                await RespondEphemeralAsync(ctx, "Es sind aktuell keine Badges definiert.");
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Alle verfügbaren Badges")
                .WithColor(DiscordColor.Azure);

            foreach (var badge in badges)
            {
                embed.AddField(
                    $"{badge.Name} ({badge.Key})",
                    string.IsNullOrWhiteSpace(badge.Description) ? "Keine Beschreibung" : badge.Description,
                    false);
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Auflisten aller Badges durch {InvokerId}.", ctx.User.Id);
            await RespondEphemeralAsync(ctx, "Beim Laden der Badges ist ein Fehler aufgetreten.");
        }
    }

    private static string FormatBadgeLine((badge Badge, DateTime GrantedAt, string? Reason) entry)
    {
        var reasonPart = string.IsNullOrWhiteSpace(entry.Reason) ? string.Empty : $", Grund: {entry.Reason}";
        return $"• {entry.Badge.Name} – {entry.Badge.Description} (seit {entry.GrantedAt:yyyy-MM-dd}{reasonPart})";
    }

    private static Task RespondEphemeralAsync(InteractionContext ctx, string message)
    {
        var response = new DiscordInteractionResponseBuilder()
            .WithContent(message)
            .AsEphemeral(true);

        return ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
    }
}
