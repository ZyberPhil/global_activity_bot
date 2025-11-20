using System;
using System.Collections.Generic;

namespace GlobalStatsBot.Models;

public partial class user
{
    public ulong Id { get; set; }

    public ulong DiscordUserId { get; set; }

    public string Username { get; set; } = null!;

    public string? Discriminator { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime FirstSeen { get; set; }

    public DateTime LastSeen { get; set; }

    public bool IsBot { get; set; }

    public bool IsBanned { get; set; }

    public ulong GlobalXpCache { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<userbadge> userbadges { get; set; } = new List<userbadge>();

    public virtual ICollection<userstat> userstats { get; set; } = new List<userstat>();
}
