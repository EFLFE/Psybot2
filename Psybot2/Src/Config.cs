using System;
using System.IO;

namespace Psybot2.Src
{
    internal static class Config
    {
        public static ulong AdminID { get; private set; }

        public static ulong LogChannelID { get; private set; }

        private const string iniPath = "config.ini";

        public const string PATH_DIR = "Publish";

        public static readonly string[] PathFiles;

        static Config()
        {
            PathFiles = new[]
            {
                "Discord.Net.Commands.dll",
                "Discord.Net.Core.dll",
                "Discord.Net.Providers.WS4Net.dll",
                "Discord.Net.Rest.dll",
                "Discord.Net.Rpc.dll",
                "Discord.Net.Webhook.dll",
                "Discord.Net.WebSocket.dll",
                "Microsoft.Extensions.DependencyInjection.dll",
                "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
                "Microsoft.Win32.Primitives.dll",
                "Newtonsoft.Json.dll",
                "System.AppContext.dll",
                "System.Collections.Immutable.dll",
                "System.Console.dll",
                "System.Diagnostics.DiagnosticSource.dll",
                "System.Globalization.Calendars.dll",
                "System.Interactive.Async.dll",
                "System.IO.Compression.dll",
                "System.IO.Compression.ZipFile.dll",
                "System.IO.FileSystem.dll",
                "System.IO.FileSystem.Primitives.dll",
                "System.Net.Http.dll",
                "System.Net.Sockets.dll",
                "System.Runtime.InteropServices.RuntimeInformation.dll",
                "System.Security.Cryptography.Algorithms.dll",
                "System.Security.Cryptography.Encoding.dll",
                "System.Security.Cryptography.Primitives.dll",
                "System.Security.Cryptography.X509Certificates.dll",
                "System.Xml.ReaderWriter.dll",
                "WebSocket4Net.dll",
                "Psybot2.exe.config",
                "Psybot2.pdb",
                "Psybot2.exe",
            };
        }

        public static void Init()
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Develop version");
            Console.ForegroundColor = ConsoleColor.Gray;
#endif

            if (!File.Exists("token"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("token file not found.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            try
            {
                if (File.Exists(iniPath))
                {
                    var txt = File.ReadAllLines(iniPath);

                    for (int i = 0; i < txt.Length; i++)
                    {
                        string text = txt[i].Trim();
                        if (text.Length == 0 || text[0] == ';' || text.IndexOf('=') == -1)
                            continue;

                        string[] nameKey = text.Split('=');
                        if (nameKey.Length != 2)
                            continue;

                        nameKey[0] = nameKey[0].Trim();
                        nameKey[1] = nameKey[1].Trim();

                        if (nameKey[0].Equals(nameof(AdminID), StringComparison.OrdinalIgnoreCase))
                        {
                            AdminID = ulong.Parse(nameKey[1]);
                        }
                        else if (nameKey[0].Equals(nameof(LogChannelID), StringComparison.OrdinalIgnoreCase))
                        {
                            LogChannelID = ulong.Parse(nameKey[1]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load config error:\n" + ex.ToString());
            }

            // check
            Console.ForegroundColor = ConsoleColor.Yellow;

            if (AdminID == 0UL)
                Console.WriteLine("AdminID not found");
            if (LogChannelID == 0UL)
                Console.WriteLine("LogChannelID not found");

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static string GetToken() => File.ReadAllText("token");

    }
}
