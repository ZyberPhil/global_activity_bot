using System;
using System.Collections.Generic;

namespace GlobalStatsBot.Models;

public partial class userbadge
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public uint BadgeId { get; set; }

    public ulong GrantedByDiscordUserId { get; set; }

    public DateTime GrantedAt { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual badge Badge { get; set; } = null!;

    public virtual user User { get; set; } = null!;
}
