using System;
using System.IO;
using System.Text;
using Discord.WebSocket;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class Meme : BaseModule, IAsset
    {
        private const string PATH = @"D:\home\img\meme\";
        private string[] images;
        private string[] paths;
        private string help;

        public Meme() : base(nameof(Meme), "meme")
        { }

        public override void OnEnable()
        {
            if (Directory.Exists(PATH))
            {
                Reload();
                base.OnEnable();
            }
            else
            {
                Log("Dir '" + PATH + "' not found.");
            }
        }

        private void Reload()
        {
            paths = Directory.GetFiles(PATH);
            images = new string[paths.Length];

            var sb = new StringBuilder();
            sb.AppendLine("Post meme image. Example: `psy meme [name]`. Name list:");

            for (int i = 0; i < paths.Length; i++)
            {
                images[i] = Path.GetFileNameWithoutExtension(paths[i]);
                sb.Append($"`{images[i]}`  ");
            }
            help = sb.ToString();
            sb.Clear();
        }

        public override async void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
            if (args == null)
            {
                var a = await mess.Author.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                await a.SendMessageAsync(help).ConfigureAwait(false);
                //Ext.DelayDeleteMessage(mess);
            }
            else
            {
                if (args[0] == "!reload")
                {
                    if (mess.Author.Id == Config.AdminID)
                    {
                        Reload();
                        //Ext.DelayDeleteMessage(mess);
                        await mess.Channel.SendMessageAsync(":ok_hand:").ConfigureAwait(false);
                    }
                    return;
                }

                string imgName = mess.Content.Substring(PsyClient.PREFIX_.Length + CommandName.Length + 1);

                // search
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].Equals(imgName, StringComparison.OrdinalIgnoreCase))
                    {
                        await mess.Channel.TriggerTypingAsync().ConfigureAwait(false);
                        await mess.Channel.SendFileAsync(paths[i]).ConfigureAwait(false);
                        //Ext.DelayDeleteMessage(mess);
                        return;
                    }
                }

                await mess.Channel.SendMessageAsync("Image not found.").ConfigureAwait(false);
            }

        }

    }
}
