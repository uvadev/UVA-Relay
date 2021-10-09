using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using UVACanvasAccess.ApiParts;

namespace UVA_Relay {
    internal static class Program {
        public static async Task Main() {
            Canvas.CanvasApi = new Api(Environment.GetEnvironmentVariable("UVA_RELAY_CANVAS_TOKEN"),
                                       "https://uview.instructure.com/api/v1/");
            
            var discord = new DiscordClient(new DiscordConfiguration {
                Token = Environment.GetEnvironmentVariable("UVA_RELAY_TOKEN") ?? throw new NullReferenceException("UVA_RELAY_TOKEN is unset"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });

            await discord.ConnectAsync();

            discord.UseInteractivity(new InteractivityConfiguration {
                AckPaginationButtons = true,
                PaginationBehaviour = PaginationBehaviour.Ignore
            });

            var testGuildId = ulong.Parse(Environment.GetEnvironmentVariable("UVA_RELAY_TEST_GUILD_ID")
                                          ?? throw new NullReferenceException("UVA_RELAY_TEST_GUILD_ID is unset"));
            
            var slash = discord.UseSlashCommands();
            slash.RegisterCommands<AppCommands>(testGuildId);
            slash.RegisterCommands<PingCommandGroup>(testGuildId);
            slash.RegisterCommands<FetchCommandGroup>(testGuildId);

            await Task.Delay(-1);
        }
    }
}