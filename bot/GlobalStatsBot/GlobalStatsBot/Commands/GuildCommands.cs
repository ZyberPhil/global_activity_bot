using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GlobalStatsBot.Services;

namespace GlobalStatsBot.Commands;

public class GuildCommands : ApplicationCommandModule
{
    private readonly GuildService _guildService;

    public GuildCommands(GuildService guildService)
    {
        _guildService = guildService ?? throw new ArgumentNullException(nameof(guildService));
    }

    [SlashCommand("guildinfo", "Zeigt Informationen zum aktuellen Server.")]
    public async Task GuildInfoAsync(InteractionContext ctx)
    {
        if (ctx.Guild is null)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Dieser Befehl kann nur auf einem Server verwendet werden.")
                    .AsEphemeral(true));
            return;
        }

        var guildEntity = await _guildService.GetOrCreateGuildAsync(
            ctx.Guild.Id,
            ctx.Guild.Name,
            ctx.Guild.IconUrl);

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"Server-Info: {ctx.Guild.Name}")
            .AddField("Server-ID", ctx.Guild.Id.ToString(), true)
            .AddField("XP-Tracking", guildEntity.IsXpEnabled == true ? "Aktiv" : "Deaktiviert", true)
            .AddField("Mitglieder", ctx.Guild.MemberCount.ToString(), true)
            .AddField("Erstellt am", ctx.Guild.CreationTimestamp.UtcDateTime.ToString("yyyy-MM-dd"), true)
            .WithThumbnail(ctx.Guild.IconUrl)
            .WithColor(DiscordColor.SpringGreen);

        await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }
}
