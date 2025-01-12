using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialSender
{
    static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        public const uint WM_COMMAND = 0x0111; // Command mode
        public const uint TOGGLE_VISIBILITY = 40258;
        public const uint CLOSE_WINAMP = 40001;
        public const uint INCREASE_VOLUME = 40058;
        public const uint DECREASE_VOLUME = 40059;
        public static IntPtr hwnd = IntPtr.Zero;

        const string cacheFile = "winamp_directory_cache.txt";
        static string directoryPath;

        static void selectInstallDir()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select Winamp install folder";
                DialogResult result = folderDialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    directoryPath = folderDialog.SelectedPath;
                    string winampPath = Path.Combine(directoryPath, "winamp.exe");

                    if (File.Exists(winampPath))
                    {
                        File.WriteAllText(cacheFile, directoryPath);
                        Console.WriteLine($"Selected Directory: {directoryPath}");
                        Console.WriteLine($"Directory saved to cache: {cacheFile}");
                        Process.Start(winampPath);
                    }
                    else
                    {
                        Console.WriteLine("winamp.exe not found in the selected directory.");
                    }
                }
            }
        }
        

        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (File.Exists(cacheFile))
            {
                directoryPath = File.ReadAllText(cacheFile);
                if (Directory.Exists(directoryPath) && File.Exists(Path.Combine(directoryPath, "winamp.exe")))
                {
                    Console.WriteLine($"Using cached directory: {directoryPath}");
                    Process.Start(Path.Combine(directoryPath, "winamp.exe"));
                } else
                {
                    Console.WriteLine("Invalid dir");
                    selectInstallDir();
                }
            }
            else
            {
                Console.WriteLine("No cache file");
                selectInstallDir();
            }

            System.Threading.Thread.Sleep(1000);
            hwnd = FindWindow("Winamp v1.x", null);
            SendMessage(hwnd, WM_COMMAND, (IntPtr)TOGGLE_VISIBILITY, IntPtr.Zero);


            

            using (ProcessIcon pi = new ProcessIcon())
            {
                pi.Display();
                Application.Run();
            }
        }
    }
}
