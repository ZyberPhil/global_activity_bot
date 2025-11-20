using System;
using System.Collections.Generic;

namespace GlobalStatsBot.Models;

public partial class guildsubscription
{
    public ulong Id { get; set; }

    public ulong GuildId { get; set; }

    public string PlanKey { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual guild Guild { get; set; } = null!;
}
