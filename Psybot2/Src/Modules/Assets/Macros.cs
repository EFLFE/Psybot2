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
            // form
            public string A { get; private set; }

            // to
            public string B { get; private set; }

            public MacroData(string a, string b)
            {
                A = a;
                B = b;
            }
        }

        private Dictionary<ulong, List<MacroData>> data =
            new Dictionary<ulong, List<MacroData>>();

        public Macros() : base(nameof(Macros), "macros")
        {
            ReceiveAllMessages = true;
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
            var dm = await mess.Author.GetOrCreateDMChannelAsync().ConfigureAwait(false);

            await dm.SendMessageAsync(
                    "Macros: auto continued your text (from -> to)(ignore case).\n" +
                    $"Set macros: `{PsyClient.PREFIX} {CommandName} set/replace [text_from] -> [text_to]`\n" +
                    $"Delete macros: `{PsyClient.PREFIX} {CommandName} delete [text_from]`\n" +
                    $"Show my macros (in PM): `{PsyClient.PREFIX} {CommandName} show`").ConfigureAwait(false);
        }

        public override void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
            if (triggered)
            {
                if (args == null || args.Length != 0)
                {
                    if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                        Help(mess);
                    if (args[0].Equals("set", StringComparison.OrdinalIgnoreCase))
                        SetMacros(args);
                    if (args[0].Equals("delete", StringComparison.OrdinalIgnoreCase))
                        DeleteMacros(args);
                }
            }
            else if (args != null && args.Length != 0)
            {
                ContinueMacros();
            }
        }

        private void SetMacros(string[] args)
        {
        }

        private void DeleteMacros(string[] args)
        {
        }

        private void ContinueMacros()
        {
        }

    }
}
