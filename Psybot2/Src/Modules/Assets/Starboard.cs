using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class Starboard : BaseModule, IAsset
    {
        private struct SBData
        {
            public ulong GuildId;
            public ulong TextChannelId;
            public string EmojiNameHook;

            public SBData(ulong guildId, ulong textChannelId, string emojiNameHook)
            {
                GuildId = guildId;
                TextChannelId = textChannelId;
                EmojiNameHook = emojiNameHook;
            }
        }

        private const string Star = "⭐";

        private List<SBData> data = new List<SBData>();

        public Starboard() : base(nameof(Starboard), "sb")
        {
            AdminOnly = true;
        }

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
        }

        public override void OnReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            ulong guildId = ((SocketGuildChannel)arg2).Id;
        }

        public override void OnReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
        }

    }
}
