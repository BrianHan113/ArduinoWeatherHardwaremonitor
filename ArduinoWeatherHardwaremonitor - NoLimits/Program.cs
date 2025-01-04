using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        
        

        [STAThread]
        static void Main()
        {
            Process.Start("D:\\winamp_install\\Winamp\\winamp.exe");
            int attempts = 0;
            int maxAttempts = 20;

            while (attempts < maxAttempts)
            {
                hwnd = FindWindow("Winamp v1.x", null);
                if (hwnd != IntPtr.Zero)
                {
                    Console.WriteLine("Winamp running.");
                    break;
                } else
                {
                    attempts++;
                    System.Threading.Thread.Sleep(500);
                }
            }

            if (attempts == maxAttempts)
            {
                Console.WriteLine("Could not open WinAmp, check directory or install");
                return;
            }

            System.Threading.Thread.Sleep(1000); // Need to let window fully load to sucessfully hide it
            SendMessage(hwnd, WM_COMMAND, (IntPtr)TOGGLE_VISIBILITY, IntPtr.Zero);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (ProcessIcon pi = new ProcessIcon())
            {
                pi.Display();
                Application.Run();
            }
        }
    }
}
