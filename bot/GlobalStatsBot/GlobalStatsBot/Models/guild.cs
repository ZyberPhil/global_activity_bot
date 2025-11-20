using System;
using System.Collections.Generic;

namespace GlobalStatsBot.Models;

public partial class guild
{
    public ulong Id { get; set; }

    public ulong DiscordGuildId { get; set; }

    public string Name { get; set; } = null!;

    public string? IconUrl { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public bool? IsXpEnabled { get; set; }

    public string? SettingsJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<guildsubscription> guildsubscriptions { get; set; } = new List<guildsubscription>();

    public virtual ICollection<userstat> userstats { get; set; } = new List<userstat>();
}
