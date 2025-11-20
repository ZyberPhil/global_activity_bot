using System;
using System.Collections.Generic;

namespace GlobalStatsBot.Models;

public partial class badge
{
    public uint Id { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? IconUrl { get; set; }

    public bool IsSystem { get; set; }

    public bool IsPremium { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<userbadge> userbadges { get; set; } = new List<userbadge>();
}
