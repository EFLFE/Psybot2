using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Discord.WebSocket;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Psybot2.Src.Modules
{
    internal sealed class ModuleManager
    {
        private readonly BaseModule[] mods;
        private readonly string helpCommands;
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
            sb.AppendLine("Avaiable commands:");

            PsyClient.CustomLog("Found " + count.ToString() + " mods");

            for (int i = 0; i < count; i++)
            {
                PsyClient.CustomLog("> " + assetList[i].Name);

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

            helpCommands = sb.ToString();
            sb.Clear();
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
                            mess.Channel.SendMessageAsync(helpCommands).Wait();
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

        public void EnableAll()
        {
            for (int i = 0; i < mods.Length; i++)
            {
                try
                {
                    mods[i]?.OnEnable();
                }
                catch (Exception exc)
                {
                    PsyClient.CustomLog($"Enable mod '{mods[i].ModName}' error: ", ex: exc);
                }
            }
        }

        public void DisableAll()
        {
            for (int i = 0; i < mods.Length; i++)
            {
                try
                {
                    mods[i]?.OnDisable();
                }
                catch (Exception exc)
                {
                    PsyClient.CustomLog($"Disable mod '{mods[i].ModName}' error: ", ex: exc);
                }
            }
        }

    }
}
