using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using GlobalStatsBot.Services;
using Microsoft.Extensions.Logging;

namespace GlobalStatsBot.Commands;

public class PingCommandModule : ApplicationCommandModule
{
    private readonly ProfileService _profileService;
    private readonly ILogger<PingCommandModule> _logger;

    public PingCommandModule(ProfileService profileService, ILogger<PingCommandModule> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    [SlashCommand("ping", "Ein einfacher Testbefehl, um den Bot zu prüfen.")]
    public async Task PingAsync(InteractionContext ctx)
    {
        var profile = await _profileService.GetUserProfileAsync(ctx.User.Id, topBadges: 0);
        var suffix = profile is null
            ? "Du hast noch keine XP gesammelt."
            : $"Dein globales XP-Konto: {profile.GlobalXp}.";

        _logger.LogInformation("/ping invoked by {UserId}", ctx.User.Id);
        await ctx.CreateResponseAsync($"Pong! 🏓 {suffix}");
    }
}