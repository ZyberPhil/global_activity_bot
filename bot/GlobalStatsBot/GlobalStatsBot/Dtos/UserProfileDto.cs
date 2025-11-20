using System;
using System.Collections.Generic;

namespace GlobalStatsBot.Dtos;

public sealed class UserProfileDto
{
    public ulong DiscordUserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public long GlobalXp { get; init; }
    public int Level { get; init; }
    public int GuildCount { get; init; }
    public IReadOnlyList<UserBadgeDto> Badges { get; init; } = Array.Empty<UserBadgeDto>();
}

public sealed class UserBadgeDto
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public DateTime GrantedAt { get; init; }
    public string? Reason { get; init; }
}
