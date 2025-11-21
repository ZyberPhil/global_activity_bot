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

// .env laden, BEVOR du etwas aus Environment/IConfiguration liest
DotEnv.Load(new DotEnvOptions(
    probeForEnv: true,
    probeLevelsToSearch: 8,
    ignoreExceptions: false
));

// Connection String direkt aus Environment holen
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("WARNUNG: DB_CONNECTION_STRING ist nicht gesetzt!");
}
else
{
    Console.WriteLine("Using connection string: " + connectionString);
}

builder.Services.AddDbContext<DiscordIdentityContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Falls du noch andere Settings aus appsettings.json nutzt:
builder.Services.Configure<DiscordBotOptions>(
    builder.Configuration.GetSection(DiscordBotOptions.SectionName));

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<GuildService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<BadgeService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddSingleton<XpMessageHandler>();
builder.Services.AddHostedService<BotService>();
builder.Services.AddHostedService<GlobalXpSyncService>();

var host = builder.Build();
await host.RunAsync();