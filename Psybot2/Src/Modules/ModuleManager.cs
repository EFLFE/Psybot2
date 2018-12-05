using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Psybot2.Src.Modules
{
    internal sealed class ModuleManager
    {
        private readonly BaseModule[] mods;
        private readonly string helpCommands;
        private readonly Embed helpEmbed;
        private readonly IPsyClient client;

        public ModuleManager(IPsyClient psy)
        {
            client = psy;
            var assetList = new List<Type>();

            Type[] types = Assembly.GetExecutingAssembly().GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];

                if (t.IsClass)
                {
                    Type interfa = t.GetInterface(nameof(IAsset), false);

                    if (interfa != null)
                    {
                        assetList.Add(t);
                    }
                }
            }

            int count = assetList.Count;
            mods = new BaseModule[count];

            var sb = new StringBuilder();

            PsyClient.CustomLog("Found " + count.ToString() + " mods");

            for (int i = 0; i < count; i++)
            {
                PsyClient.CustomLog("- " + assetList[i].Name);

                try
                {
                    BaseModule mod = (BaseModule)Activator.CreateInstance(assetList[i]);
                    mod.Init(psy);
                    mods[i] = mod;

                    if (mod.CommandName != null)
                    {
                        if (!mod.Hidden)
                        {
                            if (mod.AdminOnly)
                                sb.Append($"`{mod.CommandName}*` ");
                            else
                                sb.Append($"`{mod.CommandName}` ");
                        }
                    }
                }
                catch (Exception exc)
                {
                    PsyClient.CustomLog("Error", ex: exc);
                }
            }

            // setup help text
            helpCommands = sb.ToString();
            sb.Clear();

            var eb = new EmbedBuilder
            {
                Color = Color.Red,
                Description = helpCommands,
                Title = "Psybot² commands:",
                Url = "https://github.com/EFLFE/Psybot2",
            };
            helpEmbed = eb.Build();
        }

        public Task ClientMessageReceivedAsync(SocketMessage mess)
        {
            return Task.Run(() =>
            {
                lock (this)
                {
                    try
                    {
                        // check
                        string content = mess.Content.Trim();

                        //if (content.Length <= PsyClient.PREFIX_.Length)
                        //    return; // no commands

                        string[] args = content.Split(' ');

                        //if (args.Length < 2)
                        //    return;

                        // help ?
                        if (args.Length == 2 && args[0] == PsyClient.PREFIX && (args[1] == "help" || args[1] == "?"))
                        {
                            //mess.Channel.SendMessageAsync(helpCommands).Wait();
                            Ext.DelayDeleteMessage(mess);
                            IDMChannel dm = mess.Author.GetOrCreateDMChannelAsync().GetAwaiter().GetResult();
                            dm.SendMessageAsync(string.Empty, embed: helpEmbed).Wait();
                            return;
                        }

                        // skip prefix $ com name
                        string[] skipArgs = args.Length > 2 ? args.Skip(2).ToArray() : null;

                        for (int i = mods.Length - 1; i > -1; --i)
                        {
                            BaseModule mod = mods[i];

                            if (mod.IsEnable)
                            {
                                if (mod.AdminOnly && mess.Author.Id != Config.AdminID)
                                    continue;

                                if (mod.CommandName != null && args.Length > 1 && mod.CommandName.Equals(args[1]))
                                {
                                    mod.OnGetMessage(true, mess, skipArgs);
                                }
                                else if (mod.ReceiveAllMessages)
                                {
                                    mod.OnGetMessage(false, mess, skipArgs);
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        PsyClient.CustomLog("CMR error.", ex: exc);

                        if (client.IsConnected)
                        {
                            mess.Channel.SendMessageAsync("Error: " + exc.Message);
                        }
                    }
                }
            });
        }

        public Task ClientMessageReactionEventAsync(bool added, Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            return Task.Run(() =>
            {
                lock (this)
                {
                    try
                    {
                        for (int i = mods.Length - 1; i > -1; --i)
                        {
                            BaseModule mod = mods[i];

                            if (mod.IsEnable && mod.Reaction)
                            {
                                if (added)
                                    mod.OnReactionAdded(arg1, arg2, arg3);
                                else
                                    mod.OnReactionRemoved(arg1, arg2, arg3);
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        PsyClient.CustomLog("MM reaction error.", ex: exc);

                        if (client.IsConnected)
                        {
                            arg2.SendMessageAsync("Error: " + exc.Message);
                        }
                    }
                }
            });
        }

        public bool EnableAll()
        {
            bool ok = true;

            for (int i = 0; i < mods.Length; i++)
            {
                try
                {
                    if (mods[i] != null)
                    {
                        mods[i].OnEnable();
                        if (!mods[i].IsEnable)
                        {
                            ok = false;
                            PsyClient.CustomLog($"! Mod '{mods[i].ModName}' was not enabled.");
                        }
                    }
                }
                catch (Exception exc)
                {
                    ok = false;
                    PsyClient.CustomLog($"Enable mod '{mods[i].ModName}' error: ", ex: exc);
                }
            }

            return ok;
        }

        public bool DisableAll()
        {
            bool ok = true;

            for (int i = 0; i < mods.Length; i++)
            {
                try
                {
                    if (mods[i] != null)
                    {
                        mods[i].OnDisable();
                        if (mods[i].IsEnable)
                        {
                            ok = false;
                            PsyClient.CustomLog($"! Mod '{mods[i].ModName}' was not disabled.");
                        }
                    }
                }
                catch (Exception exc)
                {
                    ok = false;
                    PsyClient.CustomLog($"Disable mod '{mods[i].ModName}' error: ", ex: exc);
                }
            }

            return ok;
        }

    }
}
