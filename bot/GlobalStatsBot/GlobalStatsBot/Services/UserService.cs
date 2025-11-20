using GlobalStatsBot.Data;
using GlobalStatsBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlobalStatsBot.Services
{

    public class UserService
    {
        private readonly DiscordIdentityContext _context;
        private readonly ILogger<UserService> _logger;

        // Hilfstyp zum Auslesen des OUT-Parameters via SELECT
        private sealed class UserIdRow
        {
            public ulong Value { get; set; }
        }

        public UserService(DiscordIdentityContext context, ILogger<UserService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // SP via EF Core Raw SQL, danach Entity per LINQ laden/aktualisieren.
        public async Task<user> GetOrCreateUserAsync(ulong discordUserId, string username, bool isBot, string? discriminator, string? avatarUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username darf nicht leer sein.", nameof(username));

            try
            {
                // 1) SP aufrufen, OUT-Param über User-Variable auslesen
                // Hinweis: Erfordert Multiple Statements und User Variables im ConnectionString.
                var idRow = await _context.Database
                    .SqlQuery<UserIdRow>(
                        $"CALL sp_GetOrCreateUserByDiscordId({discordUserId}, {username}, {isBot}, @p_UserId); SELECT CAST(@p_UserId AS UNSIGNED) AS Value;")
                    .SingleAsync(ct);

                var userId = idRow.Value;

                // 2) Per LINQ laden
                var entity = await _context.users.FirstAsync(u => u.Id == userId, ct);

                // 3) Felder aktualisieren, die die SP nicht setzt
                bool changed = false;
                if (entity.Discriminator != discriminator)
                {
                    entity.Discriminator = discriminator;
                    changed = true;
                }
                if (entity.AvatarUrl != avatarUrl)
                {
                    entity.AvatarUrl = avatarUrl;
                    changed = true;
                }

                if (changed)
                    await _context.SaveChangesAsync(ct);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SP-Aufruf via Raw SQL fehlgeschlagen. Fallback auf reine EF/LINQ-Implementierung.");

                // Fallback: reine EF/LINQ-Variante
                var now = DateTime.UtcNow;

                var entity = await _context.users.FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId, ct);
                if (entity is null)
                {
                    entity = new user
                    {
                        DiscordUserId = discordUserId,
                        Username = username,
                        Discriminator = discriminator,
                        AvatarUrl = avatarUrl,
                        FirstSeen = now,
                        LastSeen = now,
                        IsBot = isBot
                    };

                    _context.users.Add(entity);

                    try
                    {
                        await _context.SaveChangesAsync(ct);
                    }
                    catch (DbUpdateException ex2)
                    {
                        // Race-Condition mit Unique-Index
                        _logger.LogWarning(ex2, "Konnte User {DiscordUserId} nicht erstellen (mögliches Race). Lade existierenden Datensatz.", discordUserId);
                        entity = await _context.users.FirstAsync(u => u.DiscordUserId == discordUserId, ct);
                    }

                    return entity;
                }

                bool changed = false;

                if (entity.Username != username)
                {
                    entity.Username = username;
                    changed = true;
                }

                if (entity.Discriminator != discriminator)
                {
                    entity.Discriminator = discriminator;
                    changed = true;
                }

                if (entity.AvatarUrl != avatarUrl)
                {
                    entity.AvatarUrl = avatarUrl;
                    changed = true;
                }

                entity.LastSeen = now;
                changed = true;

                if (changed)
                    await _context.SaveChangesAsync(ct);

                return entity;
            }
        }
    }
}