using DSharpPlus.Entities;
using GlobalStatsBot.Data;
using GlobalStatsBot.Dtos;
using GlobalStatsBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        DiscordChannel? channel,
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

            if (channel is not null && channel.GuildId == guild.Id)
            {
                var channelStatsEntity = await _context.channelstats
                    .FirstOrDefaultAsync(cs => cs.UserId == userEntity.Id && cs.GuildId == guildEntity.Id && cs.ChannelId == channel.Id, ct);

                if (channelStatsEntity is null)
                {
                    channelStatsEntity = new channelstat
                    {
                        UserId = userEntity.Id,
                        GuildId = guildEntity.Id,
                        ChannelId = channel.Id,
                        Xp = (ulong)xpDelta,
                        Messages = (ulong)msgDelta,
                        LastMessageAt = now
                    };

                    _context.channelstats.Add(channelStatsEntity);
                }
                else
                {
                    channelStatsEntity.Xp += (ulong)xpDelta;
                    channelStatsEntity.Messages += (ulong)msgDelta;
                    channelStatsEntity.LastMessageAt = now;
                }
            }

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

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetTopGlobalUsersAsync(int size = 10, CancellationToken ct = default)
    {
        var take = NormalizeLeaderboardSize(size);

        try
        {
            var snapshot = await _context.users
                .AsNoTracking()
                .Where(u => !u.IsBot && !u.IsBanned && u.GlobalXpCache > 0)
                .OrderByDescending(u => u.GlobalXpCache)
                .ThenBy(u => u.Id)
                .Select(u => new
                {
                    u.DiscordUserId,
                    u.Username,
                    u.GlobalXpCache,
                    Messages = _context.userstats
                        .Where(us => us.UserId == u.Id)
                        .Sum(us => (decimal?)us.Messages) ?? 0m
                })
                .Take(take)
                .ToListAsync(ct);

            return snapshot
                .Select(entry => new LeaderboardEntryDto
                {
                    DiscordUserId = entry.DiscordUserId,
                    Username = entry.Username,
                    Xp = ClampToLong(entry.GlobalXpCache),
                    Messages = ClampDecimalToLong(entry.Messages)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden des globalen Leaderboards.");
            throw;
        }
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetTopUsersByGuildAsync(ulong discordGuildId, int size = 10, CancellationToken ct = default)
    {
        if (discordGuildId == 0)
            throw new ArgumentOutOfRangeException(nameof(discordGuildId));

        var take = NormalizeLeaderboardSize(size);

        try
        {
            var guildId = await _context.guilds
                .AsNoTracking()
                .Where(g => g.DiscordGuildId == discordGuildId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync(ct);

            if (guildId == 0)
                return Array.Empty<LeaderboardEntryDto>();

            var snapshot = await _context.userstats
                .AsNoTracking()
                .Where(us => us.GuildId == guildId && us.Xp > 0)
                .OrderByDescending(us => us.Xp)
                .ThenBy(us => us.Id)
                .Join(
                    _context.users.AsNoTracking(),
                    us => us.UserId,
                    u => u.Id,
                    (us, u) => new
                    {
                        u.DiscordUserId,
                        u.Username,
                        us.Xp,
                        us.Messages
                    })
                .Take(take)
                .ToListAsync(ct);

            return snapshot
                .Select(entry => new LeaderboardEntryDto
                {
                    DiscordUserId = entry.DiscordUserId,
                    Username = entry.Username,
                    Xp = ClampToLong(entry.Xp),
                    Messages = ClampToLong(entry.Messages),
                    DiscordGuildId = discordGuildId
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden des Guild-Leaderboards für {GuildId}.", discordGuildId);
            throw;
        }
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetTopUsersByChannelAsync(
        ulong discordGuildId,
        ulong channelId,
        int size = 10,
        CancellationToken ct = default)
    {
        if (discordGuildId == 0)
            throw new ArgumentOutOfRangeException(nameof(discordGuildId));
        if (channelId == 0)
            throw new ArgumentOutOfRangeException(nameof(channelId));

        var take = NormalizeLeaderboardSize(size);

        try
        {
            var guildId = await _context.guilds
                .AsNoTracking()
                .Where(g => g.DiscordGuildId == discordGuildId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync(ct);

            if (guildId == 0)
                return Array.Empty<LeaderboardEntryDto>();

            var snapshot = await _context.channelstats
                .AsNoTracking()
                .Where(cs => cs.GuildId == guildId && cs.ChannelId == channelId && cs.Xp > 0)
                .OrderByDescending(cs => cs.Xp)
                .ThenBy(cs => cs.Id)
                .Join(
                    _context.users.AsNoTracking(),
                    cs => cs.UserId,
                    u => u.Id,
                    (cs, u) => new
                    {
                        u.DiscordUserId,
                        u.Username,
                        cs.Xp,
                        cs.Messages
                    })
                .Take(take)
                .ToListAsync(ct);

            return snapshot
                .Select(entry => new LeaderboardEntryDto
                {
                    DiscordUserId = entry.DiscordUserId,
                    Username = entry.Username,
                    Xp = ClampToLong(entry.Xp),
                    Messages = ClampToLong(entry.Messages),
                    DiscordGuildId = discordGuildId,
                    ChannelId = channelId
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden des Channel-Leaderboards für {GuildId}/{ChannelId}.", discordGuildId, channelId);
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

    private static long ClampDecimalToLong(decimal value)
        => (long)Math.Min(value, long.MaxValue);

    private static int NormalizeLeaderboardSize(int size)
        => Math.Clamp(size, 1, 25);
}
