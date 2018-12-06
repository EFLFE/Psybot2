using System;
using Discord.WebSocket;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class Choice : BaseModule, IAsset
    {
        public Choice() : base(nameof(Choice), "choice")
        { }

        public override async void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
            if (args == null || args.Length < 2)
            {
                await mess.Channel.SendMessageAsync(mess.Author.Mention + " enter also some text by a space.").ConfigureAwait(false);
            }
            else
            {
                await mess.Channel.SendMessageAsync(mess.Author.Mention + " " + args[Ext.Rnd.Next(args.Length)]).ConfigureAwait(false);
            }
        }
    }
}
