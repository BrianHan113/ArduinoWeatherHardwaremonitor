using System;
using System.IO.Ports;
using System.Diagnostics;
using System.Windows.Forms;
using SerialSender.Properties;
using System.Drawing;
using LibreHardwareMonitor;
using LibreHardwareMonitor.Hardware;
using System.Timers;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;

/// 
/// <summary>
/// New code based on serialsender from github, 25.12.2019 - Merry Christmas!
/// NoLimits
/// 
/// Code is strongly modified for Nextion display + esp32 in my case (but should work with any)
/// It produces a serial string of weather using openweathermap (replace API key - watch the metric units I use) as well as extensive hardware information (currently single GPU)
/// I managed to replace the weak openhardwaremonitor.dll with librehardwaremonitor.dll which has network and otehr stuff in it
/// As a total programming noob, this code may be improved at will
/// Code is free for all but credits for my work (3 weeks coding/learning) apprechiated
/// Advanced Serial Port Monitor can spy on the serial port after activating in the iconprocess process for debugging
/// Have Fun!
/// </summary>
/// 
namespace SerialSender
{
    //////////////////////////////////////////////////////////////////

    public class RootObject
    {
        public List<myWeather> list { get; set; }
    }
    public class myWeather
    {
        public Main main { get; set; }
        public Clouds clouds { get; set; }
        public List<Weather> weather { get; set; }
        public Wind wind { get; set; }
        public string dt_txt { get; set; }
    }
    public class Main
    {
        public float temp { get; set; }
        public float humidity { get; set; }
    }
    public class Clouds
    {
        public float all { get; set; }
    }
    public class Weather
    {
        public string description { get; set; }
    }
    public class Wind
    {
        public float speed { get; set; }
    }


    /////////////////////////////////////////////////////////////////////

    class ContextMenus
    {

        const int BAUD_RATE = 115200;
        SerialPort SelectedSerialPort;
        ContextMenuStrip menu;
        LibreHardwareMonitor.Hardware.Computer thisComputer;
        private System.Threading.Timer TimerItem;
        private System.Threading.Timer TimerItem2;
        private System.Threading.Timer TimerItem3;

        private readonly ConcurrentQueue<string> sendQueue = new ConcurrentQueue<string>();
        private bool isSending = false;
        private readonly object serialLock = new object();



        public class StateObjClass
        {
            public System.Threading.Timer TimerReference;
            public bool TimerCanceled;
        }
        public ContextMenuStrip Create()
        {
            
            thisComputer = new LibreHardwareMonitor.Hardware.Computer() { };
            thisComputer.IsCpuEnabled = true;
            thisComputer.IsGpuEnabled = true;
            //   thisComputer.IsMotherboardEnabled = true; // no sensors
            thisComputer.IsMemoryEnabled = true;
            //   thisComputer.IsControllerEnabled = true; //bugged
            thisComputer.IsNetworkEnabled = true;
           //   thisComputer.IsStorageEnabled = true; //stupid bug here
            thisComputer.Open();
            

            menu = new ContextMenuStrip();
            CreateMenuItems();
            return menu;
        }

        void CreateMenuItems()
        {
           
            ToolStripMenuItem item;
            ToolStripSeparator sep;


            item = new ToolStripMenuItem();
            item.Text = "Serial Ports";
            menu.Items.Add(item);

            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                item = new ToolStripMenuItem();
                item.Text = port;
                item.Click += new EventHandler((sender, e) => Selected_Serial(sender, e, port));
                item.Image = Resources.Serial;
                menu.Items.Add(item);
            }


            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            item = new ToolStripMenuItem();
            item.Text = "Refresh";
            item.Click += new EventHandler( (sender, e ) => InvalidateMenu(menu) );
            //item.Image = Resources.Exit;
            menu.Items.Add(item);

            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            item = new ToolStripMenuItem();
            item.Text = "Exit";
            item.Click += new System.EventHandler(Exit_Click);
            item.Image = Resources.Exit;
            menu.Items.Add(item);

        }

        void InvalidateMenu(ContextMenuStrip menu)
        {
            menu.Items.Clear();
            CreateMenuItems();
        }

        void EnqueueData(string data)
        {
            sendQueue.Enqueue(data);
            StartSending();
        }

        void StartSending()
        {
            if (isSending) return;
            isSending = true;

            Task.Run(() =>
            {
                while (sendQueue.TryDequeue(out string data))
                {
                    lock (serialLock)
                    {
                        SelectedSerialPort.Write(data);
                    }
                }
                isSending = false;
            });
        }


        void Selected_Serial(object sender, EventArgs e, string selected_port)
        {
            Console.WriteLine("Selected port");
            Console.WriteLine(selected_port);
            Console.ReadLine();
            SelectedSerialPort = new SerialPort(selected_port, BAUD_RATE);
            if ( ! SelectedSerialPort.IsOpen)
            {
                SelectedSerialPort.Open();
            };
            StateObjClass StateObj = new StateObjClass();
            StateObj.TimerCanceled = false;
            System.Threading.TimerCallback TimerDelegate = new System.Threading.TimerCallback(dataCheck);
            System.Threading.TimerCallback TimerDelegate2 = new System.Threading.TimerCallback(weatherapp);
            System.Threading.TimerCallback TimerDelegate3 = new System.Threading.TimerCallback(readSerial);

            TimerItem = new System.Threading.Timer(TimerDelegate, StateObj, 2500, 2500); //hardware
            TimerItem2 = new System.Threading.Timer(TimerDelegate2, StateObj, 5000, 30000); //weather
            TimerItem3 = new System.Threading.Timer(TimerDelegate3, StateObj, 100, 100); //Serial transmitted from esp

            StateObj.TimerReference = TimerItem;
            
        }


        /////////////////////////////////////////////////////////

        public void readSerial(object StateObj)
        {
            SelectedSerialPort.DataReceived += (sender, e) =>
            {
                // Read the line from the serial port
                string data = SelectedSerialPort.ReadLine();
                Console.WriteLine("Received: " + data);
            };
        }
        /////////////////////////////////////////////////////////


        public void weatherapp(object StateObj)
        {

            Console.WriteLine("Weather App function entered");



            string[] temps = new string[10];
            string[] humidities = new string[10];
            string[] clouds = new string[10];
            string[] sky = new string[20];
            string[] wind = new string[10];

            try
            {
                using (WebClient client = new WebClient())
                {
                    Console.WriteLine("ACCESSING jsonWeather ...");
                    client.Proxy = null;
                    string jsonWeather = client.DownloadString("http://api.openweathermap.org/data/2.5/forecast?q=Auckland,NZ&APPID=45c3e583468bf450fc17026d6734507e&units=metric");

                    var myweather = JsonConvert.DeserializeObject<RootObject>(jsonWeather);

                    int i = 0;
                    foreach (var json in myweather.list.Take(9))
                    {
                        i++;
                        temps[i] = json.main.temp.ToString();
                        humidities[i] = json.main.humidity.ToString();
                        //clouds[i] = json.clouds.all.ToString();
                        double windspeed = Math.Round(json.wind.speed * 3.6, 2);
                        wind[i] = windspeed.ToString();
                        sky[i] = json.weather.First().description.ToString();
                    }

                    
                    String datastream2 = "Current weather: " + sky[1] + " " + temps[1] + "°C " + wind[1] + "Km/h " + humidities[1] + "% " +
                                      "Forecast 3h: " + sky[2] + " " + temps[2] + "C " + wind[2] + "Km/h " + humidities[2] + "% " +
                                      "Forecast 6h: " + sky[3] + " " + temps[3] + "C " + wind[3] + "Km/h " + humidities[3] + "% " +
                                      "Forecast 9h: " + sky[4] + " " + temps[4] + "C " + wind[4] + "Km/h " + humidities[4] + "% " +
                                      "Forecast 12h: " + sky[5] + " " + temps[5] + "C " + wind[5] + "Km/h " + humidities[5] + "% " +
                                      "Forecast 15h: " + sky[6] + " " + temps[6] + "C " + wind[6] + "Km/h " + humidities[6] + "% " +
                                      "Forecast 18h: " + sky[7] + " " + temps[7] + "C " + wind[7] + "Km/h " + humidities[7] + "% " +
                                      "Forecast 21h: " + sky[8] + " " + temps[8] + "C " + wind[8] + "Km/h " + humidities[8] + "% " +
                                      "Forecast 24h: " + sky[9] + " " + temps[9] + "C " + wind[9] + "Km/h " + humidities[9] + "% " + (char)0x03;

                    Console.WriteLine(datastream2);
                    //SelectedSerialPort.Write(datastream2);
                    EnqueueData(datastream2);
                    Console.WriteLine("Weather data sent");
                }
            }
            catch (Exception)
            {

                Console.WriteLine("############################### ATTENTION: No internet connection for weather ###############################");
            }


        }
/////////////////////////////////////////////////////////
        
        public void dataCheck(object StateObj)
        {

            float GpuMemory = -1.0f;
            float GpuFan = -1.0f;
            float GpuLoad = -1.0f;
            float GpuMemoryClock = -1.0f;
            float GpuTemp = -1.0f;
            float GpuClock = -1.0f;
            float[] coreNoLoad = new float[10];
            float CpuPower = -1.0f;
            float[] coreNoTemp = new float[10];
            float[] coreNoClock = new float[10];
            float RamUsed = -1.0f;
            float RamAvail = -1.0f;
            float UploadSpeed = -1.0f;
            float DownloadSpeed = -1.0f;
            float CpuFan = -1.0f;

            //;
            StateObjClass State = (StateObjClass)StateObj;
            // enumerating all the hardware
            foreach (LibreHardwareMonitor.Hardware.IHardware hw in thisComputer.Hardware)
            {
                //Console.WriteLine("HARDWARE: " + hw.HardwareType);
                Console.ReadLine();
                
                hw.Update();
                // searching for all sensors and adding data to listbox
                foreach (LibreHardwareMonitor.Hardware.ISensor s in hw.Sensors)
                {
                    //Console.WriteLine("NAME: " + s.Name + ", TYPE: " + s.SensorType + ", VALUE: " + s.Value);
                    Console.ReadLine();
// CPU  
                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature)
                    {
                        if (s.Value != null)
                        {
                            if (s.Name.StartsWith("CPU Core #") && s.Name.Length == 11)
                            {

                                int coreid = int.Parse(s.Name.Split('#')[1]);
                                int coreIndex = coreid - 1;

                                //string corenumber = coreid.ToString();
                                //string coreNoTemp = "" + Convert.ToDouble(s.Value);
                                
                                coreNoTemp[coreIndex] = (float)Convert.ToDouble(s.Value);

                                //Console.WriteLine(coreNoTempStr[coreid]);

                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Fan)
                    {
                        if (s.Value != null)
                        {
                            float cpuFan = (float)Math.Round((double)s.Value, 2);
                            switch (s.Name)
                            {
                                case "GPU":
                                    CpuFan = cpuFan;
                                    break;
                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Power)
                    {
                        if (s.Value != null)
                        {
                             float cpuPower = (float)Math.Round((double)s.Value,2);
                             switch (s.Name)
                             {
                                 case "CPU Package":
                                    CpuPower = cpuPower;
                                     break;
                             }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock)
                    {
                        if (s.Value != null)
                        {
                            if (s.Name.StartsWith("CPU Core #") && s.Name.Length == 11)
                            {

                                int coreid = int.Parse(s.Name.Split('#')[1]);
                                int coreIndex = coreid - 1;

                                //string corenumber = coreid.ToString();
                                //string coreNoClock = "" + s.Value;

                                coreNoClock[coreIndex] = (float)s.Value;

                               // Console.WriteLine(coreNoClockStr[coreid]);

                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load)
                    {
                        if (s.Value != null)
                        {
                            if (s.Name.StartsWith("CPU Core #") && s.Name.Length == 11)
                            {

                                int coreid = int.Parse(s.Name.Split('#')[1]);
                                int coreIndex = coreid - 1;

                                //string corenumber = coreid.ToString();
                                //string coreNoLoad = "" + Math.Round(Convert.ToDouble(s.Value),2);

                                coreNoLoad[coreIndex] = (float)Math.Round(Convert.ToDouble(s.Value), 2);

                               // Console.WriteLine(coreNoLoadStr[coreid]);

                            }
                        }
                    }
// GPU
                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature)
                    {
                        if (s.Value != null)
                        {
                            float gpuTemp = (float)Math.Round((double)s.Value, 2);
                            switch (s.Name)
                            {
                                case "GPU Core":
                                    GpuTemp = gpuTemp;
                                    break;
                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock)
                    {
                        if (s.Value != null)
                        {
                            float gpuClock = (float)Math.Round((double)s.Value,2);
                            switch (s.Name)
                            {
                                case "GPU Core":
                                    GpuClock = gpuClock;
                                    break;
                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock)
                    {
                        if (s.Value != null)
                        {
                            float gpumemoryClock = (float)Math.Round((double)s.Value, 2);
                            switch (s.Name)
                            {
                                case "GPU Memory":
                                    GpuMemoryClock = gpumemoryClock;
                                    break;
                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load)
                    {
                        if (s.Value != null)
                        {
                            float gpuLoad = (float)Math.Round((double)s.Value, 2);
                            switch (s.Name)
                            {
                                case "GPU Core":
                                    GpuLoad = gpuLoad;
                                    break;
                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Fan)
                    {
                        if (s.Value != null)
                        {
                            float gpuFan = (float)Math.Round((double)s.Value, 2);
                            switch (s.Name)
                            {
                                case "GPU":
                                    GpuFan = gpuFan;
                                    break;
                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load)
                    {
                        if (s.Value != null)
                        {
                            float gpuMemory = (float)Math.Round((double)s.Value, 2);
                            switch (s.Name)
                            {
                                case "GPU Memory":
                                    GpuMemory = gpuMemory;
                                    break;
                            }
                        }
                    }


// RAM
                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Data)
                    {
                        if (s.Value != null)
                        {
                            float ramUsed = (float)Math.Round((double)s.Value, 2);
                            switch (s.Name)
                            {
                                case "Memory Used":
                                    RamUsed = ramUsed;
                                    break;
                            }
                        }
                    }
                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Data)
                    {
                        if (s.Value != null)
                        {
                            float ramAvail = (float)Math.Round((double)s.Value, 2);
                            switch (s.Name)
                            {
                                case "Memory Available":
                                    RamAvail = ramAvail;
                                    break;
                            }
                        }
                    }
// Network
                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Throughput)
                    {
                        if (s.Value != null)
                        {
                            float uploadSpeed = (float)Math.Round((double)s.Value / 2600, 2);
                            switch (s.Name)
                            {
                                case "Upload Speed":
                                    UploadSpeed = uploadSpeed;
                                    break;
                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Throughput)
                    {
                        if (s.Value != null)
                        {
                            float downloadSpeed = (float)Math.Round((double)s.Value / 2600, 2);
                            switch (s.Name)
                            {
                                case "Download Speed":
                                    DownloadSpeed = downloadSpeed;
                                    break;
                            }
                        }
                    }
                }
            }


           //String datastream = "Computer Data: " + DownloadSpeed + UploadSpeed + RamUsed + RamAvail + GpuLoad + GpuFan + GpuMemory + GpuMemoryClock + GpuClock + GpuTemp + CpuFan + CpuPower +  coreNoTempStr[1] + coreNoTempStr[2] + coreNoTempStr[3] + coreNoTempStr[4] + coreNoClockStr[1] + coreNoClockStr[2] + coreNoClockStr[3] + coreNoClockStr[4] + coreNoLoadStr[1] + coreNoLoadStr[2] + coreNoLoadStr[3] + coreNoLoadStr[4] + " END ";


            ComputerData computerData = new ComputerData
            {
                DownloadSpeed = DownloadSpeed,
                UploadSpeed = UploadSpeed,
                RamUsed = RamUsed,
                RamAvail = RamAvail,
                GpuLoad = GpuLoad,
                GpuFan = GpuFan,
                GpuMemory = GpuMemory,
                GpuMemoryClock = GpuMemoryClock,
                GpuClock = GpuClock,
                GpuTemp = GpuTemp,
                CpuFan = CpuFan,
                CpuPower = CpuPower,
                CoreNoTemp = coreNoTemp,
                CoreNoClock = coreNoClock,
                CoreNoLoad = coreNoLoad
            };

            var json = "HARDWARE" + JsonConvert.SerializeObject(computerData) + (char)0x03;

            Console.WriteLine(json);
            //SelectedSerialPort.Write(json);
            EnqueueData(json);
            Console.WriteLine("Hardware data sent");
            
        }

        void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}