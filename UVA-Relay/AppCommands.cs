using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using UVACanvasAccess.Util;
using static DSharpPlus.InteractionResponseType;
using static UVA_Relay.Canvas;
using static UVA_Relay.Utils;
using static UVACanvasAccess.ApiParts.Api;

// ReSharper disable ClassNeverInstantiated.Global
namespace UVA_Relay {
    public class AppCommands : ApplicationCommandModule {
        
    }

    [SlashCommandGroup("fetch", "Fetch commands.")]
    public class FetchCommandGroup : ApplicationCommandModule {
        
        [SlashCommand("summary", "Fetches a concise summary of a course.")]
        public async Task FetchCourseSummary(InteractionContext ctx, 
                                             [Option("courseId", "The course ID.")] long courseId) {
            await ctx.CreateResponseAsync(DeferredChannelMessageWithSource,
                                          new DiscordInteractionResponseBuilder().WithContent("Gimme a sec."));

            try {
                var course = await TimeoutThrow(
                    CanvasApi.GetCourse((ulong) courseId, includes: IndividualLevelCourseIncludes.Everything),
                    5000
                );

                var baseEmbed = MakeOkEmbed(course.Name, course.Id.ToString()).AddField("Term", course.Term?.Name ?? "n/a");
                var assignmentSummaryEmbed = MakeOkEmbed("Upcoming Due Dates");

                var assignments = await CanvasApi.StreamCourseAssignments(course.Id, AssignmentInclusions.AllDates, orderBy: "due_at")
                                                 .Where(a => a.Published)
                                                 .Where(a => a.DueAt != null)
                                                 .ToListAsync();

                if (assignments.Count == 0) {
                    assignmentSummaryEmbed.WithDescription("Nothing here!");
                }
                
                foreach (var assignment in assignments.Take(3)) {
                    Debug.Assert(assignment.DueAt != null);
                    assignmentSummaryEmbed.AddField(assignment.Name, $"Due: {assignment.DueAt.Value.FriendlyFormat()}");
                }

                if (assignments.Count > 3) {
                    assignmentSummaryEmbed.WithFooter($"And {assignments.Count - 3} more. Use '/fetch assignments' to see the rest!");
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(baseEmbed).AddEmbed(assignmentSummaryEmbed));
            } catch (Exception e) {
                Console.WriteLine(e);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(MakeGenericErrorEmbed(timeout: e is TimeoutException)));
            }
        }

        [SlashCommand("assignments", "Fetches all upcoming assignments.")]
        public async Task FetchUpcomingAssignments(InteractionContext ctx,
                                                   [Option("courseId", "The course ID.")] long courseId) {
            await ctx.CreateResponseAsync(DeferredChannelMessageWithSource,
                                          new DiscordInteractionResponseBuilder().WithContent("Gimme a sec."));

            try {
                var assignments = await CanvasApi.StreamCourseAssignments((ulong) courseId, AssignmentInclusions.AllDates, orderBy: "due_at")
                                                 .Where(a => a.Published)
                                                 .Where(a => a.DueAt != null)
                                                 .Select(a => (a.Name, a.DueAt?.FriendlyFormat()))
                                                 .ToListAsync();

                
                var pages = assignments.Chunk(3).ZipCount().Select(t => {
                    var (page, i) = t;
                    var embed = MakeOkEmbed("Due Assignments", $"Page {i + 1}");
                    foreach (var (name, due) in page) {
                        embed.AddField(name, due);
                    }
                    return new Page(embed: embed);
                });

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(MakeOkEmbed("Ok, check it out!")));
                await ctx.Interaction.Channel.SendPaginatedMessageAsync(ctx.User, pages);
            } catch (Exception e) {
                Console.WriteLine(e);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(MakeGenericErrorEmbed(timeout: e is TimeoutException)));
            }
        }
    }
    
    [SlashCommandGroup("ping", "Ping commands.")]
    public class PingCommandGroup : ApplicationCommandModule {
        
        [SlashCommand("bot", "Pings the bot.")]
        public async Task Ping(InteractionContext ctx) {
            await ctx.CreateResponseAsync(ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Pong."));
        }

        [SlashCommand("canvas", "Pings Canvas.")]
        public async Task CanvasPing(InteractionContext ctx) {
            await ctx.CreateResponseAsync(DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());

            try {
                var (good, me) = await Timeout(CanvasApi.GetUser(), 3000);
                if (good) {
                    await ctx.EditResponseAsync(
                        new DiscordWebhookBuilder().AddEmbed(MakeOkEmbed("Canvas is up!")
                                                            .AddField("I am", me.Name)
                                                            .AddField("Id", me.Id.ToString()).Build())
                    );
                } else {
                    await ctx.EditResponseAsync(
                        new DiscordWebhookBuilder().AddEmbed(MakeErrorEmbed("I'm having trouble connecting to Canvas", "Timed out after 3 seconds."))
                    );
                }
            } catch (Exception) {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddEmbed(MakeErrorEmbed("I'm having trouble connecting to Canvas", "Exception from API."))
                );
            } 
        }
    }
}