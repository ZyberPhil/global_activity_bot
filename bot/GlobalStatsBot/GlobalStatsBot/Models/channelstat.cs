using System;
using System.Collections.Generic;

namespace GlobalStatsBot.Models;

public partial class channelstat
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public ulong GuildId { get; set; }

    public ulong ChannelId { get; set; }

    public ulong Xp { get; set; }

    public ulong Messages { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual guild Guild { get; set; } = null!;

    public virtual user User { get; set; } = null!;
}
