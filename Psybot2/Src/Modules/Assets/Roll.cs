using System;
using System.Text;
using System.Linq;
using Discord.WebSocket;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class Roll : BaseModule, IAsset
    {
        private StringBuilder sb;

        public Roll() : base(nameof(Roll), "roll")
        { }

        public override async void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
            string outMess = null;

            try
            {
                if (args == null || args.Length == 0)
                {
                    outMess = "Missind arguments. Sample: `roll 2d6`";
                }
                else
                {
                    outMess = GetRoll(args[0], mess.Author.Mention);
                }
            }
            catch (Exception ex)
            {
                Log("Roll parse error.", ex);
                return;
            }

            await mess.Channel.SendMessageAsync(outMess).ConfigureAwait(false);
        }

        private string GetRoll(string arg, string who)
        {
            if (arg[0] == 'd')
            {
                // d6
                arg = "1" + arg;
            }
            if (!arg.Contains("d"))
            {
                return "Missind 'd'. Sample: `roll 2d6`";
            }
            if (arg.Count(a => a == 'd') > 1)
            {
                return "Too many 'd'.";
            }
            string[] rs = arg.Split('d');

            if (rs[0].Length == 0 || rs[1].Length == 0)
            {
                return "Bad values ​​format. Sample: `roll 2d6`";
            }

            if (rs[1].Length > 9)
            {
                return $"Too many dice edges.";
            }

            if (ulong.TryParse(rs[0], out ulong d1) && int.TryParse(rs[1], out int d2))
            {
                if (d1 < 1 || d2 < 1)
                {
                    return "The value may not be less than one. Sample: `roll 2d6`";
                }
                if (d1 > 128)
                {
                    return "Too many dice (no more 128).";
                }

                // roll | <name> rolled 10d6: 2 2 4 2 4 1 6 4 5 3  <Total: 33>
                if (sb == null)
                    sb = new StringBuilder();
                else
                    sb.Clear();
                sb.Append($"{who} rolled {arg}: ");

                ulong sum = 0;

                for (ulong i = 0; i < d1; i++)
                {
                    int r = Ext.Rnd.Next(d2) + 1;
                    sb.Append(r.ToString() + " ");
                    sum += (ulong)r;
                }

                sb.Append("(Total: ").Append(sum.ToString("N0")).Append(")");

                return sb.ToString();
            }
            return "The values ​​are not numbers. Sample: `roll 2d6`";
        }

    }
}
