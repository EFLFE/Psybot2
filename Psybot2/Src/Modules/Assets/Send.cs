using System;
using Discord.WebSocket;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class Send : BaseModule, IAsset
    {
        public Send() : base("Send message", "send")
        {
            Hidden = true;
            AdminOnly = true;
        }

        public override void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
            try
            {
                var g = ulong.Parse(args[0]);
                var c = ulong.Parse(args[1]);
                string text = string.Empty;
                for (int i = 2; i < args.Length; i++)
                {
                    text += args[i] + " ";
                }
                psybot.SendMessage(g, c, text);
            }
            catch (Exception ex)
            {
                Log("error.", ex);
            }
        }
    }
}
