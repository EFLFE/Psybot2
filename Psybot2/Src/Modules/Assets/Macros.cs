using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class Macros : BaseModule, IAsset
    {
        private struct MacroData
        {
            public string A { get; private set; }

            public string B { get; private set; }

            public MacroData(string a, string b)
            {
                A = a;
                B = b;
            }

            public void SetB(string b) => B = b;
        }

        private Dictionary<ulong, List<MacroData>> data =
            new Dictionary<ulong, List<MacroData>>();

        public Macros() : base(nameof(Macros), "macros")
        {
            ReceiveAllMessages = true;
#if !DEBUG
            Hidden = true;
#endif
        }

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        private async void Help(SocketMessage mess)
        {
            await mess.Channel.SendMessageAsync(
                    "Macros: auto replace your text (A -> B).\n" +
                    "Set macros: `macros set [A] -> [B]`\n" +
                    "Delete macros: `macros delete [A]`\n" +
                    "Show my macros (in PM): `macros show`");
        }

        public override async void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
            // TODO: Macros
            return;

            if (triggered)
            {
                #region SET DELETE

                if (args == null || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    Help(mess);
                }
                else if (args[0].Equals("set", StringComparison.OrdinalIgnoreCase))
                {
                    int sub = PsyClient.PREFIX_.Length + CommandName.Length + 1 + 3; // +set

                    if (mess.Content.Length <= sub)
                    {
                        await mess.Channel.SendMessageAsync("Missing AB arguments.");
                        return;
                    }

                    string[] mac = mess.Content.Substring(sub).Split(new string[] { "=>" }, StringSplitOptions.None);

                    if (mac.Length != 2)
                    {
                        await mess.Channel.SendMessageAsync("Missing `=>` splitter.");
                    }
                    else
                    {
                        string a = mac[0].Trim();
                        string b = mac[1].Trim();

                        if (a.Length == 0)
                        {
                            await mess.Channel.SendMessageAsync("Missing [A] argument.");
                        }
                        else if (b.Length == 0)
                        {
                            await mess.Channel.SendMessageAsync("Missing [B] argument.");
                        }
                        else
                        {
                            List<MacroData> authorMacros = null;

                            if (data.TryGetValue(mess.Author.Id, out authorMacros))
                            {
                                // проверить что макрос А уже существует
                                for (int i = 0; i < authorMacros.Count; i++)
                                {
                                    if (authorMacros[i].A.Equals(a, StringComparison.Ordinal))
                                    {
                                        // заменить B
                                        authorMacros[i].SetB(b);
                                        await mess.Channel.SendMessageAsync(":ok_hand:");
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                // новый автор
                                authorMacros = new List<MacroData>();
                                authorMacros.Add(new MacroData(a, b));
                                data.Add(mess.Author.Id, authorMacros);
                                await mess.Channel.SendMessageAsync(":ok_hand:");
                            }
                        }
                    }
                }
                else if (args[0].Equals("delete", StringComparison.OrdinalIgnoreCase))
                {
                    await mess.Channel.SendMessageAsync("TODO");
                }
                else if (args[0].Equals("show", StringComparison.OrdinalIgnoreCase))
                {
                    IDMChannel dm = await mess.Author.GetOrCreateDMChannelAsync();
                    if (dm == null)
                    {
                        await mess.Channel.SendMessageAsync("Fail to create the DM channel.");
                    }
                    else
                    {
                        await dm.SendMessageAsync("=)");
                    }
                }
                else
                {
                    Help(mess);
                }

                #endregion
            }
            else
            {
                // try replace macros

                var id = mess.Author.Id;

                if (data.TryGetValue(id, out List<MacroData> value))
                {
                    string a = mess.Content.Trim();

                    for (int i = 0; i < value.Count; i++)
                    {
                        if (value[i].A.Equals(a, StringComparison.Ordinal))
                        {
                            await mess.Channel.SendMessageAsync(value[i].B);
                            return;
                        }
                    }
                }

            }
        }

    }
}
