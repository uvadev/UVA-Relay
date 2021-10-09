using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace UVA_Relay {
    internal static class Utils {
        
        public static async Task<(bool, T)> Timeout<T>(Task<T> task, int timeoutMillis) where T: class {
            return await Task.WhenAny(task, Task.Delay(timeoutMillis)) == task ? (true, await task) 
                                                                               : (false, null);
        }
        
        public static async ValueTask<T> TimeoutThrow<T>(Task<T> task, int timeoutMillis) {
            if (await Task.WhenAny(task, Task.Delay(timeoutMillis)) == task) {
                return task.Result;
            }

            throw new TimeoutException($"Timeout after {timeoutMillis} ms.");
        }
        
        public static DiscordEmbedBuilder MakeEmbed(string title = "", string description = "") {
            return new DiscordEmbedBuilder().WithTitle(title)
                                            .WithDescription(description);
        }
        
        public static DiscordEmbedBuilder MakeOkEmbed(string title = "", string description = "") {
            return MakeEmbed(title, description).WithColor(DiscordColor.CornflowerBlue);
        }
        
        public static DiscordEmbedBuilder MakeWarningEmbed(string title = "", string description = "") {
            return MakeEmbed(title, description).WithColor(DiscordColor.Yellow);
        }

        public static DiscordEmbedBuilder MakeErrorEmbed(string title = "", string description = "") {
            return MakeEmbed(title, description).WithColor(DiscordColor.Red);
        }
        
        public static DiscordEmbedBuilder MakeGenericErrorEmbed(string title = "That didn't work.", 
                                                                string description = "Try again in a sec.", 
                                                                bool timeout = false) {
            return MakeErrorEmbed(timeout ? "That took too long." : title, description);
        }

        public static string FriendlyFormat(this DateTime dt) {
            return DateTime.Now.Year == dt.Year ? $"{dt:ddd, MMM d} at {dt:h:mm tt}" 
                                                : $"{dt:ddd, MMM d, yyyy} at {dt:h:mm tt}";
        }
        
        internal static IEnumerable<(T1, T2)> ZipT<T1, T2>(this IEnumerable<T1> l, IEnumerable<T2> r) {
            return l.Zip(r, (a, b) => (a, b));
        }
        
        internal static IEnumerable<(T, int)> ZipCount<T>(this IEnumerable<T> ie) {
            List<T> list = ie.ToList();
            return list.ZipT(Enumerable.Range(0, list.Count));
        }
    }
}