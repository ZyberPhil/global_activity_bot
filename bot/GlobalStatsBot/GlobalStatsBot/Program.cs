using dotenv.net;
using GlobalStatsBot.Configuration;
using GlobalStatsBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GlobalStatsBot.Data;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DiscordIdentityContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// .env laden: aufwärts nach .env suchen (mehrere Ebenen)
DotEnv.Load(new DotEnvOptions(
    probeForEnv: true,
    probeLevelsToSearch: 8,
    ignoreExceptions: false
));

// Konfiguration: Discord-Options binden
builder.Services.Configure<DiscordBotOptions>(
    builder.Configuration.GetSection(DiscordBotOptions.SectionName));

// Logging (konsole reicht fürs Erste)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// BotService als Hosted Service registrieren
builder.Services.AddHostedService<BotService>();

var host = builder.Build();
await host.RunAsync();