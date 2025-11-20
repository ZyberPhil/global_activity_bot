using GlobalStatsBot.Data;
using GlobalStatsBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GlobalStatsBot.Services;

public class BadgeService
{
    private readonly DiscordIdentityContext _context;
    private readonly UserService _userService;
    private readonly ILogger<BadgeService> _logger;

    public BadgeService(
        DiscordIdentityContext context,
        UserService userService,
        ILogger<BadgeService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<badge?> GetBadgeByKeyAsync(string badgeKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(badgeKey))
            throw new ArgumentException("Badge key darf nicht leer sein.", nameof(badgeKey));

        var normalizedKey = badgeKey.Trim();

        try
        {
            return await _context.badges
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Key == normalizedKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Badge {BadgeKey}.", normalizedKey);
            throw;
        }
    }

    public async Task<IReadOnlyList<badge>> GetAllBadgesAsync(CancellationToken ct = default)
    {
        try
        {
            var list = await _context.badges
                .AsNoTracking()
                .OrderBy(b => b.DisplayOrder)
                .ThenBy(b => b.Name)
                .ToListAsync(ct);

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden aller Badges.");
            throw;
        }
    }

    public async Task<bool> GiveBadgeToUserAsync(
        ulong targetDiscordUserId,
        string targetUsername,
        string badgeKey,
        ulong grantedByDiscordUserId,
        string? reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetUsername))
            throw new ArgumentException("Username darf nicht leer sein.", nameof(targetUsername));

        try
        {
            var user = await _userService.GetOrCreateUserAsync(
                targetDiscordUserId,
                targetUsername,
                isBot: false,
                discriminator: null,
                avatarUrl: null,
                ct);

            var badgeEntity = await GetBadgeByKeyAsync(badgeKey, ct);
            if (badgeEntity is null)
            {
                _logger.LogWarning("Badge mit Key {BadgeKey} existiert nicht.", badgeKey);
                return false;
            }

            var alreadyHasBadge = await _context.userbadges
                .AnyAsync(ub => ub.UserId == user.Id && ub.BadgeId == badgeEntity.Id, ct);

            if (alreadyHasBadge)
                return true;

            var newUserBadge = new userbadge
            {
                UserId = user.Id,
                BadgeId = badgeEntity.Id,
                GrantedByDiscordUserId = grantedByDiscordUserId,
                GrantedAt = DateTime.UtcNow,
                Reason = reason
            };

            _context.userbadges.Add(newUserBadge);
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Vergeben der Badge {BadgeKey} an User {DiscordUserId}.", badgeKey, targetDiscordUserId);
            throw;
        }
    }

    public async Task<IReadOnlyList<(badge Badge, DateTime GrantedAt, string? Reason)>> GetBadgesForUserAsync(
        ulong discordUserId,
        CancellationToken ct = default)
    {
        try
        {
            var userId = await _context.users
                .AsNoTracking()
                .Where(u => u.DiscordUserId == discordUserId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct);

            if (userId == 0)
                return Array.Empty<(badge, DateTime, string?)>();

            var results = await _context.userbadges
                .AsNoTracking()
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Badge)
                .OrderBy(ub => ub.Badge.DisplayOrder)
                .ThenBy(ub => ub.GrantedAt)
                .Select(ub => new { ub.Badge, ub.GrantedAt, ub.Reason })
                .ToListAsync(ct);

            return results
                .Select(r => (r.Badge, r.GrantedAt, r.Reason))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Badges für User {DiscordUserId}.", discordUserId);
            throw;
        }
    }

    public async Task<IReadOnlyList<(badge Badge, DateTime GrantedAt, string? Reason)>> GetTopBadgesForUserAsync(
        ulong discordUserId,
        int topN,
        CancellationToken ct = default)
    {
        if (topN <= 0)
            return Array.Empty<(badge, DateTime, string?)>();

        try
        {
            var userId = await _context.users
                .AsNoTracking()
                .Where(u => u.DiscordUserId == discordUserId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct);

            if (userId == 0)
                return Array.Empty<(badge, DateTime, string?)>();

            var results = await _context.userbadges
                .AsNoTracking()
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Badge)
                .OrderBy(ub => ub.Badge.DisplayOrder)
                .ThenBy(ub => ub.GrantedAt)
                .Take(topN)
                .Select(ub => new { ub.Badge, ub.GrantedAt, ub.Reason })
                .ToListAsync(ct);

            return results
                .Select(r => (r.Badge, r.GrantedAt, r.Reason))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Top-Badges für User {DiscordUserId}.", discordUserId);
            throw;
        }
    }
}
