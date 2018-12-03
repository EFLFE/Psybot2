using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Psybot2.Src.Modules.Assets
{
    internal sealed class NuCounter : BaseModule, IAsset
    {
        private sealed class NuData
        {
            public ulong Id;
            public int Count;

            public NuData(ulong id, int count)
            {
                Id = id;
                Count = count;
            }
        }

        private const string PATH = "nucounter.bin";
        private const ulong GUILD_HOOK = 325301963061329921UL;
        private readonly List<NuData> data;

        public NuCounter() : base(nameof(NuCounter), "nu")
        {
            data = new List<NuData>();
            ReceiveAllMessages = true;
            AdminOnly = true;
        }

        public override void OnEnable()
        {
            data.Clear();
            if (File.Exists(PATH))
            {
                using (FileStream stream = File.Open(PATH, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (BinaryReader br = new BinaryReader(stream))
                    {
                        int count = br.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            ulong id = br.ReadUInt64();
                            int nuCount = br.ReadInt32();
                            data.Add(new NuData(id, count));
                        }
                    }
                }
            }
            base.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            using (FileStream stream = File.Open(PATH, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    bw.Write(data.Count);
                    for (int i = 0; i < data.Count; i++)
                    {
                        bw.Write(data[i].Id);
                        bw.Write(data[i].Count);
                    }
                }
            }
            data.Clear();
        }

        public override void OnGetMessage(bool triggered, SocketMessage mess, string[] args)
        {
            if (mess.Channel is SocketGuildChannel guild)
            {
                if (guild.Guild.Id == GUILD_HOOK)
                {
                    if (triggered)
                    {
                        // show info
                        // TODO
                    }
                    else
                    {
                        if (mess.Content.StartsWith("ну ") ||
                            mess.Content.StartsWith("ну,") ||
                            mess.Content.Equals("ну", StringComparison.OrdinalIgnoreCase))
                        {
                            var aid = mess.Author.Id;

                            // check and add
                            for (int i = 0; i < data.Count; i++)
                            {
                                if (data[i].Id == aid)
                                {
                                    data[i].Count++;
                                    return;
                                }
                            }
                            Log("New nu detected");
                            data.Add(new NuData(aid, 1));
                        }
                    }
                }
            }
        }
    }
}
