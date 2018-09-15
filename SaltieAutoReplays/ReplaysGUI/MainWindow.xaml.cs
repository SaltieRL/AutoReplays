using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Reflection;
using System.Windows;

namespace ReplaysGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string UPLOAD_URL = "http://saltie.tk/replays/parse";

        Queue<string> replaysToUpload = new Queue<string>();

        public MainWindow()
        {
            InitializeComponent();

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
        }

        // Method should contain whatever should happen on replay file creation.
        private void OnFileCreate(object source, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(".replay"))
            {
                Console.WriteLine("New replay found: {0}", e.FullPath);
                replaysToUpload.Enqueue(e.FullPath);
                // We must sleep because it takes a while for Rocket League to completely save the file after creation.
                System.Threading.Thread.Sleep(2000);
                UploadReplays();
            }
        }

        public void UploadReplays()
        {
            if (AutoUpload.IsChecked ?? false)
                return;

            for (int i = 0; i < replaysToUpload.Count; i++)
            {
                UploadReplay(replaysToUpload.Dequeue());
            }
        }

        // Method should contain whatever should happen when Rocket League is started.
        private void OnProcessCreate(object source, EventArrivedEventArgs e)
        {
            if (AutoSave.IsChecked ?? false)
                return;

            foreach (var item in ((ManagementBaseObject)e.NewEvent["TargetInstance"]).Properties)
            {
                if (item.Name == "Caption" && item.Value.ToString().ToLower() == "rocketleague.exe")
                {
                    Console.WriteLine("rocketleague.exe was started");

                    try
                    {
                        Process injectorProcess = Process.Start("RLBot Injector.exe");
                        injectorProcess.EnableRaisingEvents = true;
                        injectorProcess.Exited += new EventHandler(InjectorProcessExited);
                    }
                    catch (FileNotFoundException)
                    {
                        throw new FileNotFoundException("Injector was not found in the same folder as this exe! (" +
                            Assembly.GetEntryAssembly().Location + ")");
                    }
                }
            }
        }

        private void InjectorProcessExited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int exitCode = ((Process)sender).ExitCode;

                if (exitCode == 0)
                    SavingStatus.Content = "Status: Injected successfully! Replays will be automatically saved.";
                else
                    SavingStatus.Content = $"Status: Injection failed. Exit code: {exitCode}";
            });

        }

        private void UploadReplay(string filename)
        {
            var wc = new WebClient();
            Console.WriteLine("Uploading replay file: {0}", filename);
            wc.UploadFileAsync(new Uri(UPLOAD_URL), filename);
        }
    }
}
