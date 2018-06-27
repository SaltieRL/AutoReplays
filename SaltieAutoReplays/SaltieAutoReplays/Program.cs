using System;
using System.IO;

namespace SaltieAutoReplays
{
    class Program
    {
        static void Main()
        {
            // TODO: Read the replays path from a config file instead. Could be set during installation.
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string replaysPath = Path.Combine(documentsPath, @"My Games\Rocket League\TAGame");

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = replaysPath;
            watcher.Filter = "*.replay";
            watcher.Created += new FileSystemEventHandler(OnFileCreate);
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Watching for new replays in {0}\n", replaysPath);
            Console.WriteLine("Press q to quit.");
            while (Console.ReadKey().KeyChar != 'q') ;
        }

        static void OnFileCreate(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("Created: {0}", e.FullPath);
        }
    }
}
