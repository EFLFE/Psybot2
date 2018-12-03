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
            Reload();
            base.OnEnable();
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
                await mess.Channel.SendMessageAsync(help);
            }
            else
            {
                if (args[0] == "!reload")
                {
                    if (mess.Author.Id == Config.AdminID)
                    {
                        Reload();
                        await mess.DeleteAsync();
                        await mess.Channel.SendMessageAsync(":ok_hand:");
                    }
                    return;
                }

                string imgName = mess.Content.Substring(PsyClient.PREFIX_.Length + CommandName.Length + 1);

                // search
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].Equals(imgName, StringComparison.OrdinalIgnoreCase))
                    {
                        await mess.Channel.TriggerTypingAsync();
                        await mess.DeleteAsync();
                        await mess.Channel.SendFileAsync(paths[i]);
                        return;
                    }
                }

                await mess.Channel.SendMessageAsync("Image not found.");
            }

        }

    }
}
