using GlobalStatsBot.Data;
using GlobalStatsBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace GlobalStatsBot.Services
{

    public class UserService
    {
        private readonly DiscordIdentityContext _context;
        private readonly ILogger<UserService> _logger;

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
                var userId = await TryCallGetOrCreateUserStoredProcAsync(discordUserId, username, isBot, ct);
                if (userId is null)
                    throw new InvalidOperationException("Stored Procedure returned no UserId");

                // 2) Per LINQ laden
                var entity = await _context.users.FirstAsync(u => u.Id == userId.Value, ct);

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

        public async Task<user?> GetUserByDiscordIdAsync(ulong discordUserId, CancellationToken ct = default)
        {
            try
            {
                return await _context.users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Users {DiscordUserId}.", discordUserId);
                throw;
            }
        }

        public async Task UpdateLastSeenAsync(ulong discordUserId, CancellationToken ct = default)
        {
            try
            {
                var entity = await _context.users
                    .FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId, ct);

                if (entity is null)
                {
                    _logger.LogDebug("Konnte LastSeen nicht aktualisieren. User {DiscordUserId} existiert nicht.", discordUserId);
                    return;
                }

                entity.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren von LastSeen für User {DiscordUserId}.", discordUserId);
                throw;
            }
        }

        public async Task<bool> SetUserBannedAsync(ulong discordUserId, bool isBanned, CancellationToken ct = default)
        {
            try
            {
                var entity = await _context.users
                    .FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId, ct);

                if (entity is null)
                {
                    _logger.LogDebug("Konnte Ban-Status nicht setzen. User {DiscordUserId} existiert nicht.", discordUserId);
                    return false;
                }

                if (entity.IsBanned == isBanned)
                    return true;

                entity.IsBanned = isBanned;
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Setzen des Ban-Status für User {DiscordUserId}.", discordUserId);
                throw;
            }
        }

        private async Task<ulong?> TryCallGetOrCreateUserStoredProcAsync(ulong discordUserId, string username, bool isBot, CancellationToken ct)
        {
            var connection = _context.Database.GetDbConnection();
            var closeConnection = connection.State != ConnectionState.Open;

            if (closeConnection)
                await connection.OpenAsync(ct);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "sp_GetOrCreateUserByDiscordId";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new MySqlParameter("@p_DiscordUserId", MySqlDbType.UInt64)
                {
                    Direction = ParameterDirection.Input,
                    Value = discordUserId
                });

                command.Parameters.Add(new MySqlParameter("@p_Username", MySqlDbType.VarChar, 100)
                {
                    Direction = ParameterDirection.Input,
                    Value = username
                });

                command.Parameters.Add(new MySqlParameter("@p_IsBot", MySqlDbType.Bool)
                {
                    Direction = ParameterDirection.Input,
                    Value = isBot
                });

                var outParam = new MySqlParameter("@p_UserId", MySqlDbType.UInt64)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(outParam);

                await command.ExecuteNonQueryAsync(ct);

                if (outParam.Value is null || outParam.Value == DBNull.Value)
                    return null;

                return outParam.Value switch
                {
                    ulong ulongValue => ulongValue,
                    long longValue when longValue >= 0 => (ulong)longValue,
                    decimal decimalValue when decimalValue >= 0 => (ulong)decimalValue,
                    _ => Convert.ToUInt64(outParam.Value)
                };
            }
            finally
            {
                if (closeConnection)
                    await connection.CloseAsync();
            }
        }
    }
}