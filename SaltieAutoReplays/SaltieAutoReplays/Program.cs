using System;
using System.IO;
using System.Management;
using System.Net;
using System.Diagnostics;
using System.Reflection;

namespace SaltieAutoReplays
{
    class Program
    {
        static string UPLOAD_URL = "https://localhost:5000";

        private static void Main()
        {
            // TODO: Read the replays path from a config file instead. Could be set during installation.
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string replaysPath = Path.Combine(documentsPath, @"My Games\Rocket League\TAGame\Demos");

            // Create replay folder monitor.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = replaysPath;
            watcher.Filter = "*.replay";
            watcher.Created += new FileSystemEventHandler(OnFileCreate);
            watcher.EnableRaisingEvents = true;

            // Create process monitor to watch for rocketleague.exe
            WqlEventQuery processQuery = new WqlEventQuery("__InstanceCreationEvent", new TimeSpan(0, 0, 2), "targetinstance isa 'Win32_Process'");
            ManagementEventWatcher processWatcher = new ManagementEventWatcher(processQuery);
            processWatcher.Options.Timeout = new TimeSpan(0, 1, 0);
            processWatcher.EventArrived += new EventArrivedEventHandler(OnProcessCreate);
            processWatcher.Start();

            Console.WriteLine("Watching for new replays in {0}", replaysPath);
            Console.WriteLine("Watching for Rocket League startup in order to inject ReplaySaver.dll");
            Console.WriteLine("\nPress q to quit.\n");
            while (Console.ReadKey().KeyChar != 'q') ;
        }

        // Method should contain whatever should happen on replay file creation. E.g. Upload file to server.
        private static void OnFileCreate(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("New replay found: {0}", e.FullPath);
            UploadFile(e.FullPath);
        }

        // Method should contain whatever should happen when Rocket League is started.
        private static void OnProcessCreate(object source, EventArrivedEventArgs e)
        {
            foreach (var item in ((ManagementBaseObject)e.NewEvent["TargetInstance"]).Properties)
            {
                if (item.Name == "Caption" && item.Value.ToString().ToLower() == "rocketleague.exe")
                {
                    Console.WriteLine("rocketleague.exe was started");

                    try
                    {
                        Process.Start("RLBot Injector.exe");
                        Console.WriteLine("ReplaySaver.dll was successfully injected");
                    }
                    catch (FileNotFoundException)
                    {
                        throw new FileNotFoundException("Injector was not found in the same folder as this exe! (" +
                            Assembly.GetEntryAssembly().Location + ")");
                    }
                }
            }
        }

        private static void UploadFile(string filename)
        {
            var wc = new WebClient();
            Console.WriteLine("Uploading replay file: {0}", filename);
            wc.UploadFileAsync(new Uri(UPLOAD_URL), filename);
        }
    }
}
