using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GlobalStatsBot.Services;
using Microsoft.Extensions.Logging;

namespace GlobalStatsBot.Commands;

public class ProfileCommands : ApplicationCommandModule
{
    private readonly ProfileService _profileService;
    private readonly ILogger<ProfileCommands> _logger;

    public ProfileCommands(ProfileService profileService, ILogger<ProfileCommands> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [SlashCommand("me", "Zeigt dein globales Profil.")]
    public async Task MeAsync(
        InteractionContext ctx,
        [Option("user", "Optional: Profil eines anderen Nutzers anzeigen")] DiscordUser? user = null)
    {
        var targetUser = user ?? ctx.User;

        try
        {
            var profile = await _profileService.GetUserProfileAsync(targetUser.Id, topBadges: 3);
            if (profile is null)
            {
                await ctx.CreateResponseAsync("Du hast noch keine XP gesammelt. Schreibe ein paar Nachrichten, um XP zu erhalten.");
                return;
            }

            var embed = BuildProfileEmbed(profile, targetUser);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Ausführen von /me für User {UserId}.", ctx.User.Id);
            await ctx.CreateResponseAsync("Unerwarteter Fehler beim Laden deines Profils. Bitte versuche es später erneut.");
        }
    }

    private static DiscordEmbed BuildProfileEmbed(Dtos.UserProfileDto profile, DiscordUser targetUser)
    {
        var embed = new DiscordEmbedBuilder()
            .WithTitle($"Profil von {profile.Username}")
            .WithThumbnail(targetUser.AvatarUrl ?? targetUser.DefaultAvatarUrl)
            .AddField("Global XP", profile.GlobalXp.ToString("N0", CultureInfo.InvariantCulture), true)
            .AddField("Level", profile.Level.ToString(CultureInfo.InvariantCulture), true)
            .AddField("Server", profile.GuildCount.ToString(CultureInfo.InvariantCulture), true)
            .WithColor(DiscordColor.Blurple);

        var badgeText = profile.Badges.Count == 0
            ? "Keine Badges"
            : string.Join("\n", profile.Badges.Select((b, index) =>
                $"{index + 1}. {b.Name} – {b.Description}{FormatBadgeReasonAndDate(b)}"));

        embed.AddField("Badges", badgeText, false);

        return embed.Build();
    }

    private static string FormatBadgeReasonAndDate(Dtos.UserBadgeDto badge)
    {
        var parts = Enumerable.Empty<string?>();

        if (!string.IsNullOrWhiteSpace(badge.Reason))
            parts = parts.Append(badge.Reason);

        parts = parts.Append($"seit {badge.GrantedAt:yyyy-MM-dd}");

        var suffix = string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return suffix.Length > 0 ? $" ({suffix})" : string.Empty;
    }
}
