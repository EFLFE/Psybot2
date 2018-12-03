using Discord;

namespace Psybot2.Src
{
    internal interface IPsyClient
    {
        bool IsConnected { get; }

        void SendMessageToLogChannel(string text);

        void SendMessage(ulong guildId, ulong channelId, string text, Embed embed = null);
    }
}
