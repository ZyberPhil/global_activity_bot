using DSharpPlus.Entities;
using GlobalStatsBot.Data;
using GlobalStatsBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GlobalStatsBot.Services;

public class StatsService
{
    private readonly DiscordIdentityContext _context;
    private readonly UserService _userService;
    private readonly GuildService _guildService;
    private readonly ILogger<StatsService> _logger;

    public StatsService(
        DiscordIdentityContext context,
        UserService userService,
        GuildService guildService,
        ILogger<StatsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _guildService = guildService ?? throw new ArgumentNullException(nameof(guildService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddXpForMessageAsync(
        DiscordUser discordUser,
        DiscordGuild guild,
        long xpDelta = 1,
        long msgDelta = 1,
        CancellationToken ct = default)
    {
        if (discordUser is null)
            throw new ArgumentNullException(nameof(discordUser));
        if (guild is null)
            throw new ArgumentNullException(nameof(guild));
        if (xpDelta < 0)
            throw new ArgumentOutOfRangeException(nameof(xpDelta));
        if (msgDelta < 0)
            throw new ArgumentOutOfRangeException(nameof(msgDelta));

        try
        {
            var userEntity = await _userService.GetOrCreateUserAsync(
                discordUser.Id,
                discordUser.Username,
                discordUser.IsBot,
                discordUser.Discriminator,
                discordUser.AvatarUrl,
                ct);

            var guildEntity = await _guildService.GetOrCreateGuildAsync(
                guild.Id,
                guild.Name,
                guild.IconUrl,
                ct);

            var statsEntity = await _context.userstats
                .FirstOrDefaultAsync(us => us.UserId == userEntity.Id && us.GuildId == guildEntity.Id, ct);

            var now = DateTime.UtcNow;

            if (statsEntity is null)
            {
                statsEntity = new userstat
                {
                    UserId = userEntity.Id,
                    GuildId = guildEntity.Id,
                    Xp = (ulong)xpDelta,
                    Messages = (ulong)msgDelta,
                    LastMessageAt = now
                };

                _context.userstats.Add(statsEntity);
            }
            else
            {
                statsEntity.Xp += (ulong)xpDelta;
                statsEntity.Messages += (ulong)msgDelta;
                statsEntity.LastMessageAt = now;
            }

            userEntity.GlobalXpCache += (ulong)xpDelta;

            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren der XP für User {UserId} in Guild {GuildId}.", discordUser?.Id, guild?.Id);
            throw;
        }
    }

    public async Task<long> GetGlobalXpAsync(ulong discordUserId, CancellationToken ct = default)
    {
        try
        {
            var xp = await _context.users
                .AsNoTracking()
                .Where(u => u.DiscordUserId == discordUserId)
                .Select(u => u.GlobalXpCache)
                .FirstOrDefaultAsync(ct);

            return ClampToLong(xp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Auslesen von GlobalXpCache für User {DiscordUserId}.", discordUserId);
            throw;
        }
    }

    public async Task<(long globalXp, int guildCount)> GetAggregatedStatsAsync(ulong discordUserId, CancellationToken ct = default)
    {
        try
        {
            var userId = await _context.users
                .AsNoTracking()
                .Where(u => u.DiscordUserId == discordUserId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct);

            if (userId == 0)
                return (0, 0);

            var aggregate = await _context.userstats
                .AsNoTracking()
                .Where(us => us.UserId == userId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Xp = g.Sum(us => (decimal)us.Xp),
                    GuildCount = g.Count()
                })
                .FirstOrDefaultAsync(ct);

            if (aggregate is null)
                return (0, 0);

            var xpValue = (long)Math.Min(aggregate.Xp, long.MaxValue);
            return (xpValue, aggregate.GuildCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aggregieren der Stats für User {DiscordUserId}.", discordUserId);
            throw;
        }
    }

    public Task<int> GetLevelFromXpAsync(long globalXp)
    {
        if (globalXp <= 0)
        {
            return Task.FromResult(0);
        }

        // Level requirement: XP_needed = 10 * level^2
        var level = (int)Math.Floor(Math.Sqrt(globalXp / 10d));
        return Task.FromResult(Math.Max(level, 0));
    }

    private static long ClampToLong(ulong value)
        => value > long.MaxValue ? long.MaxValue : (long)value;
}
