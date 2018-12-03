using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using Psybot2.Src.Modules;

namespace Psybot2.Src
{
    internal  sealed  class PsyClient : IPsyClient
    {
        public const string PREFIX = "psy";

        public const string PREFIX_ = "psy ";

        public const string ADMIN_PREFIX = "!psy";

        private static StringBuilder sbLog;

        private ModuleManager moduleManager;

        private TermCommands[] commands;

        private DiscordSocketClient client;

        private bool exit;

        private bool safeMode;

        private OutComEnun outCom;

        private DeletedMessageMonitor dmm;

        public bool IsConnected
        {
            get
            {
                return client != null && client.ConnectionState == ConnectionState.Connected;
            }
        }

        public PsyClient()
        {
            sbLog = new StringBuilder();
            dmm = new DeletedMessageMonitor();
            commands = new TermCommands[]
            {
                new TermCommands("help", "Show commands.", new Action(Help), null),
                new TermCommands("exit", "Exit.", new Action(Exit), null),
                new TermCommands("start", "Login and connect.", new Action(Start), null),
                new TermCommands("update", "Update app.", new Action(Update), null),
                new TermCommands("publish", "Publish app.", new Action(Publish), null),
                new TermCommands("safemode", "Publish app.", delegate()
                {
                    safeMode = !safeMode;
                }, null)
            };
        }

        public void Publish()
        {
            outCom = OutComEnun.Publish;
            exit = true;
        }

        public void Update()
        {
            outCom = OutComEnun.Update;
            exit = true;
        }

        private void Exit()
        {
            CustomLog("Exit command!", CustomLogEnum.Psybot, null);
            exit = true;
        }

        private void Help()
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
                Start();
            }
            ThreadPool.QueueUserWorkItem(delegate (object _)
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
            wasConnected = IsConnected;
            ModuleManager moduleManager = this.moduleManager;
            if (moduleManager != null)
            {
                moduleManager.DisableAll();
            }
            if (client != null && client.ConnectionState == ConnectionState.Connected)
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
                    if (commands[i].Excecute(ref cmd[0]))
                    {
                        return true;
                    }
                }
                Console.WriteLine("Command '" + cmd[0] + "' not found.");
                return false;
            }
            return false;
        }

        private void Start()
        {
            if (moduleManager == null)
            {
                CustomLog("Init ModuleManager", CustomLogEnum.Psybot, null);
                moduleManager = new ModuleManager(this);
                moduleManager.EnableAll();
            }
            if (client == null)
            {
                CustomLog("Init Discord.Net", CustomLogEnum.Psybot, null);
                DiscordSocketConfig conf = new DiscordSocketConfig
                {
                    WebSocketProvider = WS4NetProvider.Instance,
                    ConnectionTimeout = 9999,
                    LogLevel = LogSeverity.Verbose
                };
                client = new DiscordSocketClient(conf);
                client.Log += Client_Log;
                client.MessageReceived += Client_MessageReceived;
                client.MessageDeleted += Client_MessageDeleted;
                client.ReactionAdded += Client_ReactionAdded;
                client.ReactionRemoved += Client_ReactionRemoved;
            }
            client.LoginAsync(TokenType.Bot, Config.GetToken(), true).Wait();
            client.StartAsync().Wait();
        }

        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            return moduleManager.ClientMessageReactionEventAsync(true, arg1, arg2, arg3);
        }

        private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
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
            }
        }

        private async Task Client_MessageReceived(SocketMessage mess)
        {
            if (mess.Source == MessageSource.User)
            {
                dmm.NewMessage(mess);
                if (!safeMode || mess.Author.Id == Config.AdminID)
                {
                    if (mess.Content == "psy ping")
                    {
                        await mess.Channel.SendMessageAsync("bot pong", false, null, null);
                    }
                    else if (mess.Content.StartsWith("!psy"))
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

        private Task Client_MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            dmm.MessageDeleted(arg1.Id);
            return Task.CompletedTask;
        }

        private Task Client_Log(LogMessage arg)
        {
            StringBuilder obj = sbLog;
            lock (obj)
            {
                Console.WriteLine(arg.ToString(sbLog, true, true, DateTimeKind.Local, new int?(11)));
            }
            return Task.CompletedTask;
        }

        private async void ParseAdminCom(SocketMessage mess)
        {
            if (mess.Content.Length > "!psy".Length + 2)
            {
                if (!ExcecuteCommand(mess.Content.Remove(0, "!psy".Length + 1)))
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
