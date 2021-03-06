﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using Psybot2.Src.GeneralModules;
using Psybot2.Src.Modules;

namespace Psybot2.Src
{
    internal sealed class PsyClient : IPsyClient
    {
        public const string PREFIX = "psy";
        public const string PREFIX_ = "psy ";
        public const string ADMIN_PREFIX = "!psy";
        private const int queueLogCap = 30;

        private static StringBuilder sbLog;
        private static Queue<string> queueLog;

        private ModuleManager moduleManager;
        private TermCommands[] commands;
        private DiscordSocketClient client;
        private bool exit;
        private bool safeMode;
        private OutComEnun outCom;
        private BlackList blackList;

        public bool IsConnected
        {
            get
            {
                return client?.ConnectionState == ConnectionState.Connected;
            }
        }

        public PsyClient()
        {
            sbLog = new StringBuilder();
            blackList = new BlackList();
            queueLog = new Queue<string>(queueLogCap);

            commands = new TermCommands[]
            {
                new TermCommands("help", "Show commands.", Help),
                new TermCommands("exit", "Exit.", Exit),
                new TermCommands("start", "Login and connect.", Start),
                new TermCommands("update", "Update app.", Update),
                new TermCommands("publish", "Publish app.", Publish),
                new TermCommands("safemode", "On/off safe mode (only admin commands).", (_) =>
                {
                    safeMode = !safeMode;
                    CustomLog("Safe mode is " + (safeMode ? "on" : "off"));
                }),
                new TermCommands("log", "Send log to admin.", async (_) =>
                {
                    if (queueLog.Count > 0 && IsConnected)
                    {
                        string mess = string.Empty;

                        lock (sbLog)
                        {
                            sbLog.Clear();
                            sbLog.Append("```");
                            while (queueLog.Count > 0)
                                sbLog.AppendLine(queueLog.Dequeue());
                            sbLog.Append("```");
                            mess = sbLog.ToString();
                            sbLog.Clear();

                            if (mess.Length > 2000)
                                mess = mess.Remove(2000);
                        }

                        try
                        {
                            IDMChannel dm = await client.GetUser(Config.AdminID).GetOrCreateDMChannelAsync();
                            await dm.SendMessageAsync(mess);
                        }
                        catch (Exception ex)
                        {
                            CustomLog("Fail to send log.", ex: ex);
                        }
                    }
                }),
                new TermCommands("bl", "Black list.", (args) =>
                {
                    if (args.Length == 3 && ulong.TryParse(args[2], out ulong id))
                    {
                        if (args[1] == "add")
                        {
                            blackList.Add(id);
                            CustomLog("BlackList id added");
                        }
                        else if(args[1] == "remove")
                        {
                            blackList.Remove(id);
                            CustomLog("BlackList id removed");
                        }
                        else
                        {
                            CustomLog("Unknown command");
                        }
                    }
                }),
            };
        }

        public void Publish(string[] args)
        {
            if (Ext.IsLinux)
            {
                Console.WriteLine("Not support in linux.");
            }
            else
            {
                outCom = OutComEnun.Publish;
                exit = true;
            }
        }

        public void Update(string[] args)
        {
            if (Ext.IsLinux)
            {
                Console.WriteLine("Not support in linux.");
            }
            else
            {
                outCom = OutComEnun.Update;
                exit = true;
            }
        }

        private void Exit(string[] args)
        {
            CustomLog("Exit command!", CustomLogEnum.Psybot, null);
            exit = true;
        }

        private void Help(string[] args)
        {
            Console.WriteLine();
            for (int i = 0; i < commands.Length; i++)
            {
                Console.WriteLine("*  " + commands[i].GetTrigger + " | " + commands[i].GetInfo);
            }
            Console.WriteLine();
        }

        public OutComEnun Run(bool resume, out bool wasConnected)
        {
            if (resume)
            {
                Start(null);
            }
            else
            {
                Help(null);
            }

            // чтение ввод данных на фоновый поток
            ThreadPool.QueueUserWorkItem((_) =>
            {
                for (; ; )
                {
                    ExcecuteCommand(Console.ReadLine());
                }
            });

            while (!exit)
            {
                Thread.Sleep(200);
            }

            // unload
            blackList.SaveData();
            wasConnected = IsConnected;
            ModuleManager moduleManager = this.moduleManager;
            moduleManager?.DisableAll();

            if (client?.ConnectionState == ConnectionState.Connected)
            {
                client.LogoutAsync().Wait();
            }
            return outCom;
        }

        private bool ExcecuteCommand(string comName)
        {
            string[] cmd = comName.Trim().Split(new char[] { ' ' });

            if (cmd[0].Length != 0)
            {
                for (int i = 0; i != commands.Length; i++)
                {
                    if (commands[i].Excecute(cmd))
                    {
                        return true;
                    }
                }
                Console.WriteLine("Command '" + cmd[0] + "' not found.");
                return false;
            }
            return false;
        }

        private void Start(string[] arg)
        {
            if (IsConnected)
                return;

            if (moduleManager == null)
            {
                CustomLog("Init ModuleManager", CustomLogEnum.Psybot, null);
                moduleManager = new ModuleManager(this);
                if (!moduleManager.EnableAll())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Re-enter 'start' for continue");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }
            }
            if (client == null)
            {
                CustomLog("Init Discord.Net", CustomLogEnum.Psybot, null);
                var conf = new DiscordSocketConfig
                {
                    WebSocketProvider = WS4NetProvider.Instance,
                    ConnectionTimeout = 9999,
                    LogLevel = LogSeverity.Verbose
                };
                client = new DiscordSocketClient(conf);
                client.Log += Client_Log;
                client.MessageReceived += Client_MessageReceived;
                client.ReactionAdded += Client_ReactionAdded;
                client.ReactionRemoved += Client_ReactionRemoved;
                //client.UserJoined += Client_UserJoined;
                //client.UserLeft += Client_UserLeft;
                client.UserBanned += Client_UserBanned;
                client.UserUnbanned += Client_UserUnbanned;
                client.Ready += Client_Ready;
            }
            try
            {
                client.LoginAsync(TokenType.Bot, Config.GetToken(), true).Wait();
                client.StartAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection fail: " + ex.Message);
                //exit = true;
            }
        }

        private Task Client_UserUnbanned(SocketUser arg1, SocketGuild arg2)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (arg1.IsBot)
                    {
                        arg2.DefaultChannel.SendMessageAsync("User " + arg1.Username + " was unbanned!").Wait();
                    }
                }
                catch (Exception ex)
                {
                    CustomLog("Error.", ex: ex);
                }
            });
        }

        private Task Client_UserBanned(SocketUser arg1, SocketGuild arg2)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (arg1.IsBot)
                    {
                        arg2.DefaultChannel.SendMessageAsync("User " + arg1.Username + " was banned!").Wait();
                    }
                }
                catch (Exception ex)
                {
                    CustomLog("Error.", ex: ex);
                }
            });
        }

        private Task Client_UserLeft(SocketGuildUser arg)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (arg.IsBot)
                    {
                        arg.Guild.DefaultChannel.SendMessageAsync("Bye, " + arg.Username + " !").Wait();
                    }
                }
                catch (Exception ex)
                {
                    CustomLog("Error.", ex: ex);
                }
            });
        }

        private Task Client_UserJoined(SocketGuildUser arg)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (arg.IsBot)
                    {
                        arg.Guild.DefaultChannel.SendMessageAsync("Welcome, " + arg.Username + " !").Wait();
                    }
                }
                catch (Exception ex)
                {
                    CustomLog("Error.", ex: ex);
                }
            });
        }

        private Task Client_Ready()
        {
            return client.SetGameAsync("psy help");
        }

        private async Task Client_MessageReceived(SocketMessage mess)
        {
            if (mess.Source == MessageSource.User)
            {
                if (blackList.Contains(mess.Author.Id))
                    return;

                if (!safeMode || mess.Author.Id == Config.AdminID)
                {
                    if (mess.Content == "psy ping")
                    {
                        await mess.Channel.SendMessageAsync("bot pong", false, null, null);
                    }
                    else if (mess.Content.StartsWith(ADMIN_PREFIX))
                    {
                        if (mess.Author.Id == Config.AdminID)
                        {
                            ParseAdminCom(mess);
                        }
                    }
                    else
                    {
                        await moduleManager.ClientMessageReceivedAsync(mess);
                    }
                }
            }
        }

        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (blackList.Contains(arg3.User.Value.Id))
                return Task.CompletedTask;

            return moduleManager.ClientMessageReactionEventAsync(true, arg1, arg2, arg3);
        }

        private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (blackList.Contains(arg3.User.Value.Id))
                return Task.CompletedTask;

            return moduleManager.ClientMessageReactionEventAsync(false, arg1, arg2, arg3);
        }

        public static void CustomLog(string message, CustomLogEnum source = CustomLogEnum.Psybot, Exception ex = null)
        {
            StringBuilder obj = sbLog;
            lock (obj)
            {
                string mess = new LogMessage(LogSeverity.Info, source.ToString(), message, ex).ToString(sbLog, true, true, DateTimeKind.Local, new int?(11));
                if (ex != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(mess);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else if (Console.ForegroundColor != (ConsoleColor)source)
                {
                    Console.ForegroundColor = (ConsoleColor)source;
                    Console.WriteLine(mess);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.WriteLine(mess);
                }

                if (queueLog.Count == queueLogCap)
                    queueLog.Dequeue();
                queueLog.Enqueue(mess);
            }
        }

        private Task Client_Log(LogMessage arg)
        {
            StringBuilder obj = sbLog;
            lock (obj)
            {
                string text = arg.ToString(sbLog, true, true, DateTimeKind.Local, new int?(11));
                Console.WriteLine(text);

                if (queueLog.Count == queueLogCap)
                    queueLog.Dequeue();
                queueLog.Enqueue(text);
            }
            return Task.CompletedTask;
        }

        private async void ParseAdminCom(SocketMessage mess)
        {
            if (mess.Content.Length > ADMIN_PREFIX.Length + 2)
            {
                if (!ExcecuteCommand(mess.Content.Remove(0, ADMIN_PREFIX.Length + 1)))
                {
                    await mess.Channel.SendMessageAsync("Command not found.", false, null, null);
                }
                if (exit)
                {
                    Program.PauseOnExit = true;
                }
            }
        }

        public async void SendMessageToLogChannel(string text)
        {
            await client.GetGuild(82151967899516928UL).GetTextChannel(Config.LogChannelID).SendMessageAsync(text);
        }

        public async void SendMessage(ulong guildId, ulong channelId, string text, Embed embed = null)
        {
            await client.GetGuild(guildId).GetTextChannel(channelId).SendMessageAsync(text, embed: embed);
        }
    }
}
