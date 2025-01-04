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
        public static IntPtr hwnd;

        [STAThread]
        static void Main()
        {
            Process.Start("D:\\winamp_install\\Winamp\\winamp.exe");


            System.Threading.Thread.Sleep(5000);
            hwnd = FindWindow("Winamp v1.x", null);
            
            if (hwnd != IntPtr.Zero)
            {
                Console.WriteLine("Winamp running.");
                SendMessage(hwnd, WM_COMMAND, (IntPtr)TOGGLE_VISIBILITY, IntPtr.Zero);
            } else
            {
                Console.WriteLine("Winamp not installed, or directory is incorrect");
            }

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
