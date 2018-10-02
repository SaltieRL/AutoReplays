﻿using IWshRuntimeLibrary;
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
        const string UPLOAD_URL = "https://calculated.gg/api/upload";
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
        }


        // Method should contain whatever should happen on replay file creation.
        private void OnFileCreate(object source, FileSystemEventArgs e)
        {
            replaysToUpload.Enqueue(e.FullPath);
            // We must sleep because it takes a while for Rocket League to completely save the file after creation.
            System.Threading.Thread.Sleep(2000);
            UploadReplays();
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


        private void StartInjection()
        {
            Process injectorProcess = Process.Start("RLBot Injector.exe");
            SavingStatus.Content = "Status: Injecting.";
            injectorProcess.EnableRaisingEvents = true;
            injectorProcess.Exited += new EventHandler(InjectorProcessExited);
        }

        // Method should contain whatever should happen when Rocket League is started.
        private void OnProcessCreate(object source, EventArrivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (AutoSave.IsChecked ?? false)
                    return;

                foreach (var item in ((ManagementBaseObject)e.NewEvent["TargetInstance"]).Properties)
                {
                    if (item.Name == "Caption" && item.Value.ToString().ToLower() == "rocketleague.exe")
                    {
                        try
                        {
                            StartInjection();
                        }
                        catch (FileNotFoundException)
                        {
                            WarningText.Text = "Injector was not found in the same folder as this exe!" +
                                $"({Assembly.GetEntryAssembly().Location})";
                        }
                    }
                }
            });
        }


        private void InjectorProcessExited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int exitCode = ((Process)sender).ExitCode;

                if (exitCode == 0)
                {
                    SavingStatus.Content = "Status: Injected successfully! Replays will be automatically saved.";
                    AutoSave.IsChecked = true;
                }
                else
                {
                    SavingStatus.Content = $"Status: Injection failed. Exit code: {exitCode}";
                    AutoSave.IsChecked = false;
                }
            });
        }


        private void UploadReplay(string filename)
        {
            var wc = new WebClient();
            wc.UploadFileAsync(new Uri(UPLOAD_URL), filename);
        }


        private void AutoUpload_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (AutoUpload.IsChecked ?? true)
                {
                    UploadReplays();
                    UploadingStatus.Content = "Status: Automatic uploading is enabled.";
                }
                else
                    UploadingStatus.Content = "Status: Automatic uploading is disabled.";
            });
        }


        private void AddShortcutToStartup()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, "AutoReplays.lnk");

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = Assembly.GetEntryAssembly().Location;
            shortcut.Save();
        }


        private void RemoveShortcutFromStartup()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, "AutoReplays.lnk");

            if (System.IO.File.Exists(shortcutPath))
            {
                try
                {
                    System.IO.File.Delete(shortcutPath);
                }
                catch (IOException)
                {
                    WarningText.Text = "AutoReplays could not be removed from Windows startup.";
                }
            }
        }


        private void StartOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            AddShortcutToStartup();
        }


        private void StartOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            RemoveShortcutFromStartup();
        }


        private bool IsModuleLoaded(string moduleName, string processName)
        {
            Process[] processes = Process.GetProcessesByName("RocketLeague");

            if (processes.Length == 1)
            {
                foreach (ProcessModule module in processes[0].Modules)
                {
                    if (module.ModuleName == "ReplaySaver.dll")
                        return true;
                }
            }
            return false;
        }


        private void AutoSave_Unchecked(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (IsModuleLoaded("ReplaySaver.dll", "RocketLeague.exe"))
                {
                    WarningText.Text = "Auto Saving has already been injected! Please restart Rocket League to disable it.";
                }
            });
        }


        private void AutoSave_Checked(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!IsModuleLoaded("ReplaySaver.dll", "RocketLeague.exe"))
                    SavingStatus.Content = "Status: Waiting for Rocket League to launch.";

                if (Process.GetProcessesByName("RocketLeague").Length == 1)
                    StartInjection();
            });
        }
    }
}
