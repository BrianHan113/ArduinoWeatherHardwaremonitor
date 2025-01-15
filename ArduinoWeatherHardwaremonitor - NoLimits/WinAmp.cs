using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialSender
{
    public static class WinAmp
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
        public static string directoryPath;

        public static void SelectInstallDir()
        {
            while (true)
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
                            break; // Exit the loop once valid input is provided
                        }
                        else
                        {
                            Console.WriteLine("winamp.exe not found in the selected directory. Please try again.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No directory selected. Exiting...");
                        break; // Exit if the user cancels the dialog
                    }
                }
            }
        }


        public static bool SetupWinamp()
        {
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
                    SelectInstallDir();
                }
            }
            else
            {
                Console.WriteLine("No cache file");
                SelectInstallDir();
            }


            int attempts = 0;
            int maxAttempts = 20;

            while (attempts < maxAttempts)
            {
                hwnd = FindWindow("Winamp v1.x", null);
                if (hwnd != IntPtr.Zero)
                {
                    Console.WriteLine("Winamp running.");
                    break;
                }
                else
                {
                    attempts++;
                    System.Threading.Thread.Sleep(500);
                }
            }

            if (attempts == maxAttempts)
            {
                Console.WriteLine("Could not open WinAmp, check directory or install");
            }


            System.Threading.Thread.Sleep(1000); // need to wait for window to load to hide it
            hwnd = FindWindow("Winamp v1.x", null);
            SendMessage(hwnd, WM_COMMAND, (IntPtr)TOGGLE_VISIBILITY, IntPtr.Zero);
            return true;
        }
    }
}
