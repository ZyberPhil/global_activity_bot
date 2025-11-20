using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using GlobalStatsBot.Models;

namespace GlobalStatsBot.Data;

public partial class DiscordIdentityContext : DbContext
{
    public DiscordIdentityContext(DbContextOptions<DiscordIdentityContext> options)
        : base(options)
    {
    }

    public virtual DbSet<badge> badges { get; set; }

    public virtual DbSet<guild> guilds { get; set; }

    public virtual DbSet<guildsubscription> guildsubscriptions { get; set; }

    public virtual DbSet<user> users { get; set; }

    public virtual DbSet<userbadge> userbadges { get; set; }

    public virtual DbSet<userstat> userstats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<badge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.Key, "UX_Badges_Key").IsUnique();

            entity.Property(e => e.Id).HasColumnType("int(10) unsigned");
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayOrder).HasColumnType("int(11)");
            entity.Property(e => e.IconUrl).HasMaxLength(500);
            entity.Property(e => e.Key).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt)
                .HasMaxLength(6)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp(6)");
        });

        modelBuilder.Entity<guild>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.JoinedAt, "IX_Guilds_JoinedAt");

            entity.HasIndex(e => e.DiscordGuildId, "UX_Guilds_DiscordGuildId").IsUnique();

            entity.Property(e => e.Id).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.DiscordGuildId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.IconUrl).HasMaxLength(500);
            entity.Property(e => e.IsXpEnabled)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.JoinedAt).HasMaxLength(6);
            entity.Property(e => e.LeftAt).HasMaxLength(6);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.SettingsJson).HasColumnType("json");
            entity.Property(e => e.UpdatedAt)
                .HasMaxLength(6)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp(6)");
        });

        modelBuilder.Entity<guildsubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.GuildId, "IX_GuildSubscriptions_GuildId");

            entity.HasIndex(e => e.PlanKey, "IX_GuildSubscriptions_PlanKey");

            entity.Property(e => e.Id).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.GuildId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.PlanKey).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt)
                .HasMaxLength(6)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.ValidFrom).HasMaxLength(6);
            entity.Property(e => e.ValidTo).HasMaxLength(6);

            entity.HasOne(d => d.Guild).WithMany(p => p.guildsubscriptions)
                .HasForeignKey(d => d.GuildId)
                .HasConstraintName("FK_GuildSubscriptions_Guilds");
        });

        modelBuilder.Entity<user>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.FirstSeen, "IX_Users_FirstSeen");

            entity.HasIndex(e => e.LastSeen, "IX_Users_LastSeen");

            entity.HasIndex(e => e.DiscordUserId, "UX_Users_DiscordUserId").IsUnique();

            entity.Property(e => e.Id).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.DiscordUserId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.Discriminator).HasMaxLength(10);
            entity.Property(e => e.FirstSeen).HasMaxLength(6);
            entity.Property(e => e.GlobalXpCache).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.LastSeen).HasMaxLength(6);
            entity.Property(e => e.UpdatedAt)
                .HasMaxLength(6)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<userbadge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.BadgeId, "IX_UserBadges_BadgeId");

            entity.HasIndex(e => e.UserId, "IX_UserBadges_UserId");

            entity.HasIndex(e => new { e.UserId, e.BadgeId }, "UX_UserBadges_User_Badge").IsUnique();

            entity.Property(e => e.Id).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.BadgeId).HasColumnType("int(10) unsigned");
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.GrantedAt).HasMaxLength(6);
            entity.Property(e => e.GrantedByDiscordUserId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnType("bigint(20) unsigned");

            entity.HasOne(d => d.Badge).WithMany(p => p.userbadges)
                .HasForeignKey(d => d.BadgeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserBadges_Badges");

            entity.HasOne(d => d.User).WithMany(p => p.userbadges)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserBadges_Users");
        });

        modelBuilder.Entity<userstat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.GuildId, "IX_UserStats_GuildId");

            entity.HasIndex(e => e.LastMessageAt, "IX_UserStats_LastMessageAt");

            entity.HasIndex(e => e.UserId, "IX_UserStats_UserId");

            entity.HasIndex(e => new { e.UserId, e.GuildId }, "UX_UserStats_User_Guild").IsUnique();

            entity.Property(e => e.Id).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.GuildId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.LastMessageAt).HasMaxLength(6);
            entity.Property(e => e.Messages).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.UpdatedAt)
                .HasMaxLength(6)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.UserId).HasColumnType("bigint(20) unsigned");
            entity.Property(e => e.Xp).HasColumnType("bigint(20) unsigned");

            entity.HasOne(d => d.Guild).WithMany(p => p.userstats)
                .HasForeignKey(d => d.GuildId)
                .HasConstraintName("FK_UserStats_Guilds");

            entity.HasOne(d => d.User).WithMany(p => p.userstats)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserStats_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
