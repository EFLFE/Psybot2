using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Psybot2.Src.EF;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class Starboard : BaseModule, IAsset
    {
        private const string STAR = "⭐";
#if DEBUG
        private const int GOAL = 2;
#else
        private const int GOAL = 3;
#endif

        private const string STARED_MESSAGES = "sb1.bin";
        private const string STARBOARD_CHANNELS = "sb2.bin";

        // guild Id -> channel 'starboard' id
        private Dictionary<ulong, ulong> guildChannelId = new Dictionary<ulong, ulong>();

        private List<ulong> addedMessages = new List<ulong>();

        private QueueArray<ulong> addedMessageBuffer = new QueueArray<ulong>(8);

        private BinaryStream staredMessageStream;
        private BinaryStream starboardChannelsStream;

        public Starboard() : base(nameof(Starboard), "sb")
        {
            AdminOnly = true;
            Reaction = true;

            staredMessageStream = new BinaryStream(STARED_MESSAGES, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            starboardChannelsStream = new BinaryStream(STARBOARD_CHANNELS, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            //Log("read " + STARBOARD_CHANNELS);
            if (starboardChannelsStream.FStream.Length != 0)
            {
                int sbCount = starboardChannelsStream.Reader.ReadInt32();

                for (int i = 0; i < sbCount; i++)
                {
                    var guildId = starboardChannelsStream.Reader.ReadUInt64();
                    var channelId = starboardChannelsStream.Reader.ReadUInt64();
                    guildChannelId.Add(guildId, channelId);
                }
            }
        }

        private void AddStarMessage(ulong id)
        {
            Log("AddStarMessage");
            // +1
            if (staredMessageStream.FStream.Length == 0)
            {
                staredMessageStream.Writer.Write(1);
            }
            else
            {
                staredMessageStream.FStream.Seek(0L, 0);
                int count = staredMessageStream.Reader.ReadInt32();
                staredMessageStream.FStream.Seek(0L, 0);
                staredMessageStream.Writer.Write(count + 1);
            }

            // +id
            staredMessageStream.FStream.Seek(0L, SeekOrigin.End);
            staredMessageStream.Writer.Write(id);
            staredMessageStream.FStream.Flush(true);
        }

        private bool ContainsStarMessage(ulong id)
        {
            Log("stream ContainsStarMessage..");

            if (staredMessageStream.FStream.Length == 0)
                return false;

            staredMessageStream.FStream.Seek(0L, 0);
            int count = staredMessageStream.Reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                ulong id2 = staredMessageStream.Reader.ReadUInt64();
                if (id == id2)
                {
                    Log("found");
                    return true;
                }
            }

            Log("not found");
            return false;
        }

        private void AddGuildChannelSb(ulong guildId, ulong channelId)
        {
            Log("AddGuildChannelSb");
            // +1
            if (starboardChannelsStream.FStream.Length == 0)
            {
                starboardChannelsStream.Writer.Write(1);
            }
            else
            {
                starboardChannelsStream.FStream.Seek(0L, 0);
                int count = starboardChannelsStream.Reader.ReadInt32();
                starboardChannelsStream.FStream.Seek(0L, 0);
                starboardChannelsStream.Writer.Write(count + 1);
            }

            // +id
            starboardChannelsStream.FStream.Seek(0L, SeekOrigin.End);
            starboardChannelsStream.Writer.Write(guildId);
            starboardChannelsStream.Writer.Write(channelId);
            starboardChannelsStream.FStream.Flush(true);
        }

        private void RewriteGuildChannelSb()
        {
            starboardChannelsStream.FStream.Seek(0L, 0);
            starboardChannelsStream.Writer.Write(guildChannelId.Count);

            foreach (KeyValuePair<ulong, ulong> item in guildChannelId)
            {
                starboardChannelsStream.Writer.Write(item.Key);
                starboardChannelsStream.Writer.Write(item.Value);
            }
        }

        public override async void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
            var guildId = mess.Channel.AsGuildChannel().GuildId;

            if (args == null)
            {
                await mess.Channel.SendMessageAsync("Starboard commands: `set [channel id]`, `remove`.");
            }
            else if (args[0] == "set")
            {
                if (args.Length == 1)
                {
                    await mess.Channel.SendMessageAsync("Missing channeld id.");
                }
                else if (ulong.TryParse(args[1], out ulong sbChanneldId))
                {
                    if (guildChannelId.ContainsKey(guildId))
                    {
                        // replace
                        //guildChannelId[guildId] = sbChanneldId;
                        await mess.Channel.SendMessageAsync("Starboard already added.");
                        return;
                    }
                    else
                    {
                        // add
                        guildChannelId.Add(guildId, sbChanneldId);
                    }

                    try
                    {
                        AddGuildChannelSb(guildId, sbChanneldId);
                        await mess.Channel.TriggerTypingAsync();
                        Ext.DelayDeleteMessage(mess);
                        await mess.Channel.SendMessageAsync("Starboard added :ok_hand:");
                    }
                    catch (Exception ex)
                    {
                        guildChannelId.Remove(guildId);
                        await mess.Channel.SendMessageAsync("Error: " + ex.Message);
                        Log("Error on save data.", ex);
                        return;
                    }
                }
                else
                {
                    await mess.Channel.SendMessageAsync("Channel not found.");
                    return;
                }
            }
            else if (args[0] == "remove")
            {
                if (guildChannelId.ContainsKey(guildId))
                {
                    // replace
                    guildChannelId.Remove(guildId);

                    try
                    {
                        RewriteGuildChannelSb();
                    }
                    finally
                    {
                        Ext.DelayDeleteMessage(mess);
                        await mess.Channel.SendMessageAsync("Starboard removed :ok_hand:");
                    }
                }
            }
            else
            {
                await mess.Channel.SendMessageAsync("Unknown command.");
            }
        }

        public override void OnReactionAdded(
            Cacheable<IUserMessage, ulong> cachedMessage,
            ISocketMessageChannel mesChannel,
            SocketReaction reaction)
        {
            if (reaction.Emote.Name != STAR)
                return;

            IGuildChannel guild = mesChannel.AsGuildChannel();

            // уже имеется в списке добавленных серверов
            if (guildChannelId.TryGetValue(guild.GuildId, out ulong sbChannelId))
            {
                IUserMessage message = cachedMessage.GetOrDownloadAsync().GetAwaiter().GetResult();

                // защита от рекурсии
               // if (message.Channel.Id == sbChannelId)
               // {
               //     Log("рек");
               //     return;
               // }

                var messageId = message.Id;

                // сообщение ещё не было добавлено в сб ии имеет достаточно звёзд
                if (!addedMessageBuffer.Contains(messageId)
                    && !ContainsStarMessage(messageId)
                    && message.Reactions[reaction.Emote].ReactionCount >= GOAL)
                {
                    addedMessageBuffer.Insert(messageId);
                    AddStarMessage(messageId);

                    try
                    {
                        var builder = new EmbedBuilder()
                            .WithColor(Color.LightOrange)
                            .WithAuthor(message.Author)
                            .WithDescription(message.Content)
                            .WithTimestamp(message.Timestamp);

                        // If there is image attached
                        if (message.Attachments.Count > 0)
                            builder.ImageUrl = message.Attachments.FirstOrDefault()?.Url;

                        // If there is link thumbnail
                        if (message.Embeds.Count > 0)
                            builder.ImageUrl = message.Embeds.FirstOrDefault()?.Thumbnail.Value.Url;

                        // Star by: users..
                        IUser[] users = message
                            .GetReactionUsersAsync(STAR, GOAL)

                            .GetAwaiter()
                            .GetResult()
                            .ToArray();

                        string userSet = " Stared by: ";

                        for (int i = 0; i < users.Length; i++)
                        {
                            userSet += $", `{users[i].Username}`";
                        }

                        Embed embed = builder.Build();

                        psybot.SendMessage(
                            guild.GuildId,
                            sbChannelId,
                            //(message.Channel as ITextChannel).Mention + " ID: " + messageId.ToString(),
                            (message.Channel as ITextChannel)?.Mention + userSet,
                            embed);
                    }
                    catch (Exception ex)
                    {
                        Log("Error.", ex);
                        psybot.SendMessageToLogChannel(ex.ToString());
                    }
                }
            }

        }

    }
}
