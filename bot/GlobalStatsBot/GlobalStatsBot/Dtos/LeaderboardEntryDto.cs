namespace GlobalStatsBot.Dtos;

public sealed class LeaderboardEntryDto
{
    public ulong DiscordUserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public long Xp { get; init; }
    public long Messages { get; init; }
    public ulong? DiscordGuildId { get; init; }
    public ulong? ChannelId { get; init; }
}
