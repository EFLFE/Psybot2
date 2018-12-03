using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class Starboard : BaseModule, IAsset
    {
        private const string STAR = "⭐";
#if DEBUG
        private const int GOAL = 1;
#else
        private const int GOAL = 3;
#endif

        // guild Id -> channel 'starboard' id
        private Dictionary<ulong, ulong> guildChannelId = new Dictionary<ulong, ulong>();
        private List<ulong> addedMessages = new List<ulong>();

        public Starboard() : base(nameof(Starboard), "sb")
        {
            AdminOnly = true;
            Reaction = true;
        }

        public override void OnEnable()
        {
            LoadData();
            base.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            SaveData();
        }

        private void SaveData()
        {
        }

        private void LoadData()
        {
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
                        guildChannelId[guildId] = sbChanneldId;
                    }
                    else
                    {
                        // add
                        guildChannelId.Add(guildId, sbChanneldId);
                    }

                    try
                    {
                        SaveData();
                        await mess.Channel.TriggerTypingAsync();
                        await mess.DeleteAsync();
                        await mess.Channel.SendMessageAsync("Starboard added :ok_hand:");
                    }
                    catch (Exception ex)
                    {
                        guildChannelId.Remove(guildId);
                        await mess.Channel.SendMessageAsync("Error: " + ex.Message);
                        Log("[sb] Error on save data.", ex);
                    }
                }
                else
                {
                    await mess.Channel.SendMessageAsync("Channel not found.");
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
                        SaveData();
                    }
                    finally
                    {
                        await mess.DeleteAsync();
                        await mess.Channel.SendMessageAsync("Starboard removed :ok_hand:");
                    }
                }
            }
            else
            {
                await mess.Channel.SendMessageAsync("Unknown command.");
            }
        }

        public override async void OnReactionAdded(
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
                if (message.Channel.Id == sbChannelId)
                    return;

                // сообщение ещё не было добавлено в сб ии имеет достаточно звёзд
                if (!addedMessages.Contains(message.Id) && message.Reactions[reaction.Emote].ReactionCount >= GOAL)
                {
                    addedMessages.Add(message.Id);

                    try
                    {
                        var builder = new EmbedBuilder()
                            .WithColor(Color.LightOrange)
                            .WithAuthor(message.Author)
                            .WithDescription(message.Content)
                            .WithTimestamp(message.Timestamp);

                        // If there is image attached
                        if (message.Attachments.Count > 0)
                            builder.ImageUrl = message.Attachments.FirstOrDefault().Url;

                        // If there is link thumbnail
                        if (message.Embeds.Count > 0)
                            builder.ImageUrl = message.Embeds.FirstOrDefault().Thumbnail.Value.Url;

                        Embed embed = builder.Build();

                        psybot.SendMessage(
                            guild.GuildId,
                            sbChannelId,
                            (message.Channel as ITextChannel).Mention + " ID: " + message.Id.ToString(),
                            embed);
                    }
                    catch (Exception ex)
                    {
                        Log("[sb] error", ex);
                        psybot.SendMessageToLogChannel(ex.ToString());
                    }
                }
            }

        }

    }
}
