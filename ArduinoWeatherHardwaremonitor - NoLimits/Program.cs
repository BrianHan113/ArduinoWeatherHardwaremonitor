using System;
using System.Windows.Forms;

namespace SerialSender
{
    class Program
    {
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            WinAmp.SetupWinamp();
            Console.WriteLine(WinAmp.directoryPath);


            using (ProcessIcon pi = new ProcessIcon())
            {
                pi.Display();
                Application.Run();
            }
        }
    }
}
