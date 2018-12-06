using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace Psybot2.Src
{
    internal static class Program
    {
        public static int BuildVersion { get; private set; }

        public static bool IsDebugMode { get; private set; }

        private static PsyClient psyClient;

        private const string COM_PUBLISH = "publish";
        private const string COM_PUBLISH_AND_RESUME = "publish_resume";
        private const string COM_RESUME = "resume";

        public static bool PauseOnExit;

        private static void Main(string[] args)
        {
            Console.Title = "Psybot 2";
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Psybot 2 ");

            GetBuildVersion();
            Console.WriteLine("build " + BuildVersion.ToString());
            Console.Title += " v" + BuildVersion.ToString();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("\nEnvironment dir: " + Environment.CurrentDirectory);

            bool resume = false;

            if (args?.Length > 0)
            {
                if (args[0] == COM_RESUME)
                {
                    resume = true;
                }
                else if (args[0] == COM_PUBLISH || args[0] == COM_PUBLISH_AND_RESUME)
                {
                    try
                    {
                        // создание новой сборки и её запуск
                        Publish(args[0] == COM_PUBLISH_AND_RESUME);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        Console.ReadLine();
                    }
                    return;
                }
            }

            Config.Init();
            Console.WriteLine();

            bool useTryCatch =
#if DEBUG
                !Debugger.IsAttached
#else
                true
#endif
                ;

            IsDebugMode = !useTryCatch;

            if (useTryCatch)
            {
                try
                {
                    psyClient = new PsyClient();
                    OutComEnun com = psyClient.Run(resume, out bool wasConnected);

                    if (com != OutComEnun.JustExit)
                    {
#if DEBUG
                        ShowUPWarning();
                        return;
#else
                        if (com == OutComEnun.Publish)
                            Publish(wasConnected);
                        else if (com == OutComEnun.Update)
                            Update(wasConnected);
#endif
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Crash: " + ex.ToString());
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine("\n------------\n" + ex.InnerException.ToString());
                    }
                    Console.ReadLine();
                }
            }
            else
            {
                psyClient = new PsyClient();

                if (psyClient.Run(resume, out bool wasConnected) != OutComEnun.JustExit)
                {
                    ShowUPWarning();
                }
            }

            if (PauseOnExit)
            {
                Console.WriteLine("\npause");
                Console.ReadLine();
            }
        }

        private static void GetBuildVersion()
        {
            var assLocal = Assembly.GetExecutingAssembly().Location;
            var ver = FileVersionInfo.GetVersionInfo(assLocal).FileVersion;
            BuildVersion = Version.Parse(ver).Revision;
        }

        private static void ShowUPWarning()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Update or publish only in Release version.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ReadLine();
        }

#if !DEBUG
        private static void Update(bool wasConnected)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nUpdate...");

            string dir = Environment.CurrentDirectory;
            dir = dir.Substring(0, dir.Length - Config.PATH_DIR.Length); // dir back

            //Console.WriteLine(dir);
            //Console.ReadLine();

            ProcessStartInfo startInfo = new ProcessStartInfo(
                dir + "Psybot2.exe", wasConnected ? COM_PUBLISH_AND_RESUME : COM_PUBLISH)
            {
                WorkingDirectory = dir,
            };
            Process.Start(startInfo);
        }
#endif

        private static void Publish(bool resume)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nPublish...");

            string outPath = Environment.CurrentDirectory + "\\" + Config.PATH_DIR + "\\";
            Directory.CreateDirectory(outPath);

            for (int i = 0; i < Config.PathFiles.Length; i++)
            {
                File.Copy(Config.PathFiles[i], outPath + Config.PathFiles[i], true);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(outPath + "Psybot2.exe", resume ? COM_RESUME : null)
            {
                // +dir_path
                WorkingDirectory = Environment.CurrentDirectory + "\\" + Config.PATH_DIR,
            };
            Process.Start(startInfo);
        }

    }
}
