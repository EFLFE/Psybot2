using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Psybot2.Src
{
    internal sealed class DeletedMessageMonitor
    {
        private struct MessageData
        {
            public string Date;
            public string Text;
            public string Author;
            public ulong Id;

            public void Set(string text, ulong id, string date, string author)
            {
                Text = text;
                Id = id;
                Date = date;
                Author = author;
            }
        }

        private const int BUFFER = 200;
        private const string BUFFER_FILE = "deleted_mess.txt";

        private MessageData[] data;
        private int index;
        private int cap;

        public DeletedMessageMonitor()
        {
            data = new MessageData[BUFFER];
        }

        public void NewMessage(SocketMessage mess)
        {
            data[index].Set(mess.Content, mess.Id, mess.Timestamp.ToString(), mess.Author.Username);

            index++;
            cap++;
            if (index == BUFFER)
                index = 0;
        }

        public void MessageDeleted(ulong id)
        {
            for (int i = 0; i < cap && i < data.Length; i++)
            {
                if (data[i].Id == id)
                {
                    File.AppendAllText(
                        BUFFER_FILE,
                        $"{data[i].Date} by {data[i].Author}: {data[i].Text}{Environment.NewLine}");
                    return;
                }
            }
        }

    }
}
