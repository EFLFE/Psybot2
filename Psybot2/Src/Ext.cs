using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Psybot2.Src
{
    internal static class Ext
    {
        public static Random Rnd = new Random();

        public static IGuildChannel AsGuildChannel(this ISocketMessageChannel socketMessageChannel)
        {
            return (IGuildChannel)socketMessageChannel;
        }

        /// <summary> Удалить сообщение через определённое время (15 сек). </summary>
        public static async void DelayDeleteMessage(SocketMessage mess)
        {
            await Task.Run(() =>
            {
                Thread.Sleep(1000 * 15);
                try
                {
                    mess.DeleteAsync().Wait();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        PsyClient.CustomLog("DelayDeleteMessage fail: " + ex.InnerException.Message);
                    else
                        PsyClient.CustomLog("DelayDeleteMessage fail: " + ex.Message);
                }
            }).ConfigureAwait(false);
        }

        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
    }
}
