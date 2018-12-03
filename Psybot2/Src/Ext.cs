using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Psybot2.Src
{
    internal static class Ext
    {
        public static Random Rnd = new Random();

        /// <summary> Удалить сообщение через определённое время. </summary>
        public static async void DelayDeleteMessage(SocketMessage mess)
        {
            await Task.Run(() =>
            {
                Thread.Sleep(1000 * 15);
                mess.DeleteAsync().Wait();
            });
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
