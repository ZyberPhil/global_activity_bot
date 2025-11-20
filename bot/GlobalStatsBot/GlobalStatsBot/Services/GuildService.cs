using GlobalStatsBot.Data;
using GlobalStatsBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GlobalStatsBot.Services;

public class GuildService
{
    private readonly DiscordIdentityContext _context;
    private readonly ILogger<GuildService> _logger;

    public GuildService(DiscordIdentityContext context, ILogger<GuildService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<guild> GetOrCreateGuildAsync(ulong discordGuildId, string name, string? iconUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Guild name darf nicht leer sein.", nameof(name));

        try
        {
            var entity = await _context.guilds
                .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId, ct);

            if (entity is null)
            {
                entity = new guild
                {
                    DiscordGuildId = discordGuildId,
                    Name = name,
                    IconUrl = iconUrl,
                    JoinedAt = DateTime.UtcNow,
                    IsXpEnabled = true
                };

                _context.guilds.Add(entity);
                await _context.SaveChangesAsync(ct);
                return entity;
            }

            var changed = false;

            if (!string.Equals(entity.Name, name, StringComparison.Ordinal))
            {
                entity.Name = name;
                changed = true;
            }

            if (entity.IconUrl != iconUrl)
            {
                entity.IconUrl = iconUrl;
                changed = true;
            }

            if (changed)
                await _context.SaveChangesAsync(ct);

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Registrieren der Guild {DiscordGuildId}.", discordGuildId);
            throw;
        }
    }

    public async Task<guild?> GetGuildByDiscordIdAsync(ulong discordGuildId, CancellationToken ct = default)
    {
        try
        {
            return await _context.guilds
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Guild {DiscordGuildId}.", discordGuildId);
            throw;
        }
    }

    public async Task<bool> SetXpEnabledAsync(ulong discordGuildId, bool isEnabled, CancellationToken ct = default)
    {
        try
        {
            var entity = await _context.guilds
                .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId, ct);

            if (entity is null)
            {
                _logger.LogDebug("Konnte XP-Flag nicht setzen. Guild {DiscordGuildId} existiert nicht.", discordGuildId);
                return false;
            }

            if (entity.IsXpEnabled == isEnabled)
                return true;

            entity.IsXpEnabled = isEnabled;
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Setzen des XP-Flags für Guild {DiscordGuildId}.", discordGuildId);
            throw;
        }
    }

    public async Task<string?> GetSettingsJsonAsync(ulong discordGuildId, CancellationToken ct = default)
    {
        try
        {
            return await _context.guilds
                .AsNoTracking()
                .Where(g => g.DiscordGuildId == discordGuildId)
                .Select(g => g.SettingsJson)
                .FirstOrDefaultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Auslesen der Settings für Guild {DiscordGuildId}.", discordGuildId);
            throw;
        }
    }
}
