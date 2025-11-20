using System.Threading.Tasks;
using DSharpPlus.SlashCommands;

namespace GlobalStatsBot.Commands;

public class PingCommandModule : ApplicationCommandModule
{
    [SlashCommand("ping", "Ein einfacher Testbefehl, um den Bot zu prüfen.")]
    public async Task PingAsync(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync("Pong! 🏓");
    }
}