using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GlobalStatsBot.Dtos;
using Microsoft.Extensions.Logging;

namespace GlobalStatsBot.Services;

public class ProfileService
{
    private readonly UserService _userService;
    private readonly StatsService _statsService;
    private readonly BadgeService _badgeService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        UserService userService,
        StatsService statsService,
        BadgeService badgeService,
        ILogger<ProfileService> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
        _badgeService = badgeService ?? throw new ArgumentNullException(nameof(badgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(
        ulong discordUserId,
        int topBadges = 3,
        CancellationToken ct = default)
    {
        if (discordUserId == 0)
            throw new ArgumentOutOfRangeException(nameof(discordUserId));

        if (topBadges < 0)
            topBadges = 0;

        try
        {
            var userEntity = await _userService.GetUserByDiscordIdAsync(discordUserId, ct);
            if (userEntity is null)
                return null;

            var (globalXp, guildCount) = await _statsService.GetAggregatedStatsAsync(discordUserId, ct);
            var level = await _statsService.GetLevelFromXpAsync(globalXp);

            IReadOnlyList<UserBadgeDto> badgeDtos = Array.Empty<UserBadgeDto>();
            if (topBadges > 0)
            {
                var badges = await _badgeService.GetTopBadgesForUserAsync(discordUserId, topBadges, ct);
                badgeDtos = badges
                    .Select(b => new UserBadgeDto
                    {
                        Key = b.Badge.Key,
                        Name = b.Badge.Name,
                        Description = b.Badge.Description,
                        IconUrl = b.Badge.IconUrl,
                        GrantedAt = b.GrantedAt,
                        Reason = b.Reason
                    })
                    .ToList();
            }

            return new UserProfileDto
            {
                DiscordUserId = userEntity.DiscordUserId,
                Username = userEntity.Username,
                GlobalXp = globalXp,
                Level = level,
                GuildCount = guildCount,
                Badges = badgeDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aufbau des Profils für User {DiscordUserId}.", discordUserId);
            throw;
        }
    }
}
