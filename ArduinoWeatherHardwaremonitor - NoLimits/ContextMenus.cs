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
using System.IO;
using System.Net.Http;



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
        public City city { get; set; }
    }

    public class City
    {
        public string name { get; set; }
        public string country { get; set; }
    }
    public class myWeather
    {
        public Main main { get; set; }
        public Wind wind { get; set; }
        public List<Weather> weather { get; set; }
    }
    public class Main
    {
        public float temp { get; set; }
    }
    public class Weather
    {
        public string icon { get; set; }
    }
    public class Wind
    {
        public float speed { get; set; }
        public int deg { get; set; }
    }


    public class RootTideObject
    {
        public List<Tide> data { get; set; }
    }

    public class Tide
    {
        public String time { get; set; }
    }


    /////////////////////////////////////////////////////////////////////

    public class ContextMenus
    {

        const int BAUD_RATE = 115200;
        SerialPort SelectedSerialPort;
        ContextMenuStrip menu;
        LibreHardwareMonitor.Hardware.Computer thisComputer;
        private System.Threading.Timer TimerItem;
        private System.Threading.Timer TimerItem2;
        private System.Threading.Timer TimerItem3;
        private System.Threading.Timer TimerItem4;
        private System.Threading.Timer TimerItem5;

        private static readonly ConcurrentQueue<string> sendQueue = new ConcurrentQueue<string>();
        private bool isSending = false;
        private readonly object serialLock = new object();
        private readonly object sendingLock = new object();
        private static readonly int maxQueueSize = 10;

        private static List<string> songs = new List<string>();

        // Set default lat long to Auckland, NZ
        private static double latitude = -36.747;
        private static double longitude = 174.739;

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
            item.Text = "WinAmp Dir";
            item.Click += (sender, e) => WinAmp.SelectInstallDir();
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

        public static void EnqueueData(string data)
        {
            if (sendQueue.Count >= maxQueueSize)
            {
                Console.WriteLine("Queue is full. Discarding item.");
                foreach (var item in sendQueue)
                {
                    Console.WriteLine(item);
                }

                return;
            }

            sendQueue.Enqueue(data);
        }

        public void sendData(object StateObj)
        {
            if (sendQueue.TryDequeue(out string data))
            {
                while (SelectedSerialPort.BytesToWrite > 0)
                {
                    Thread.Sleep(10);
                }
                lock (serialLock)
                {
                    Console.WriteLine("Sending");
                    SelectedSerialPort.Write(data);
                }
            }
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
            System.Threading.TimerCallback TimerDelegate4 = new System.Threading.TimerCallback(sendData);
            System.Threading.TimerCallback TimerDelegate5 = new System.Threading.TimerCallback(tideData);


            TimerItem = new System.Threading.Timer(TimerDelegate, StateObj, 1000, 2500); //hardware
            TimerItem2 = new System.Threading.Timer(TimerDelegate2, StateObj, 5000, 5*60*1000); //weather - free api calls abosulte min is 86.4 secs per call
            TimerItem3 = new System.Threading.Timer(TimerDelegate3, StateObj, 1000, 1000); //Serial transmitted from esp
            TimerItem4 = new System.Threading.Timer(TimerDelegate4, StateObj, 1000, 1000); //Send serial
            TimerItem5 = new System.Threading.Timer(TimerDelegate5, StateObj, 5000, 6*60*60*1000); //High Low tide

            StateObj.TimerReference = TimerItem;
            
        }


        /////////////////////////////////////////////////////////

        public void readSerial(object StateObj)
        {
            SelectedSerialPort.DataReceived += (sender, e) =>
            {
                // Read the line from the serial port
                string data = SelectedSerialPort.ReadLine();
                data = data.TrimEnd('\r');
                Console.WriteLine("Received: " + data);

                //foreach (char c in data)
                //{
                //    Console.WriteLine($"Char: {c} ASCII: {(int)c}");
                //}

                if (data == "REFRESHWEATHER")
                {
                    Console.WriteLine("Refreshing Weather info");
                    weatherapp(null);
                } else if (data == "LOCKPC")
                {
                    Console.WriteLine("Lock PC");
                    Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
                }
                else if (data == "REFRESHMUSIC") 
                {
                    SendSongString("D:\\2024-25-Summer-Internship\\risos-internship\\music");
                } else if (data.StartsWith("PLAYMUSIC"))
                {
                    String num = data.Substring(9, data.Length - 9);
                    int index = int.Parse(num);
                    String songDir;
                    try
                    {
                        songDir = songs[index];
                        Process.Start("D:\\winamp_install\\Winamp\\winamp.exe", "\"" + songDir + "\"");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Refresh music player nextion");
                    }
                    
                } else if (data == "PAUSEMUSIC")
                {
                    Process.Start("D:\\winamp_install\\Winamp\\winamp.exe", "/pause");
                }
                else if (data == "INCREASEMUSIC")
                {
                    WinAmp.SendMessage(WinAmp.hwnd, WinAmp.WM_COMMAND, (IntPtr)WinAmp.INCREASE_VOLUME, IntPtr.Zero);

                }
                else if (data == "DECREASEMUSIC")
                {
                    WinAmp.SendMessage(WinAmp.hwnd, WinAmp.WM_COMMAND, (IntPtr)WinAmp.DECREASE_VOLUME, IntPtr.Zero);
                } 
                else if (data.StartsWith("SCHEDULE"))
                {
                    if (data.Substring(8).StartsWith("CLEAR"))
                    {
                        String clearString = data.Substring(13);
                        String start = clearString.Substring(0, 4);
                        String end = clearString.Substring(4, 4);
                        String SW = clearString.Substring(8);
                        Console.WriteLine("Clearing: " + SW + start + end);
                        Scheduler.CancelTask(SW + start + end);
                        Scheduler.CancelTask(SW + start + end + "END");
                    } else
                    {
                        //Console.WriteLine("Schedule commands");
                        String start = data.Substring(8, 4);
                        String end = data.Substring(12, 4);
                        String SW = data.Substring(16);

                        Scheduler.ScheduleSwitch(SW, start, end);
                        //Scheduler.ScheduleSwitch(SW, "1618", "1619");

                        //Scheduler.ScheduleSwitch("MOTIONSENSOR", "0124", "0125");
                        //Scheduler.ScheduleSwitch("SW1", "2259", "2300");
                        //Scheduler.ScheduleSwitch("SW2", "2259", "2300");


                        //Console.WriteLine(start + " " + end + " " + SW);
                    }
                }
                else if (data.StartsWith("LOCATION")) 
                {
                    String location = data.Substring(8);
                    int latStart = location.IndexOf("LAT") + 3;
                    int latEnd = location.IndexOf("LONG");
                    latitude = double.Parse(location.Substring(latStart, latEnd - latStart));

                    int longStart = latEnd + 4;
                    longitude = double.Parse(location.Substring(longStart));

                    Console.WriteLine($"Latitude: {latitude}, Longitude: {longitude}");
                }
            };
        }

        /////////////////////////////////////////////////////////

        public static void SendSongString(string directoryPath)
        {
            songs.Clear();
            var files = Directory.GetFiles(directoryPath, "*.m4a");
            var songOptions = string.Join("BREAK", files.Select(f => Path.GetFileName(f)));
            songs.AddRange(files);

            Console.WriteLine(songOptions);
            EnqueueData("MUSICSTRING" + songOptions + (char)0x03);
        }
        /////////////////////////////////////////////////////////

        public String windDegToDirection(int deg)
        {
            // Deg from openweatherapi is meteorological, 0 is north, 90 is east, etc
            if (deg >= 0 && deg <= 22 || deg > 337 && deg <= 360)
            {
                return "Northerly";
            }
            else if (deg > 22 && deg <= 67)
            {
                return "North-Easterly";
            }
            else if (deg > 67 && deg <= 112)
            {
                return "Easterly";
            }
            else if (deg > 112 && deg <= 157)
            {
                return "South-Easterly";
            }
            else if (deg > 157 && deg <= 202)
            {
                return "Southerly";
            }
            else if (deg > 202 && deg <= 247)
            {
                return "South-Westerly";
            }
            else if (deg > 247 && deg <= 292)
            {
                return "Westerly";
            }
            else if (deg > 292 && deg <= 337)
            {
                return "North-Westerly";
            } else
            {
                return "Error, out of bounds";
            }
        }

        public void weatherapp(object StateObj)
        {

            Console.WriteLine("Weather App function entered");

            try
            {
                using (WebClient client = new WebClient())
                {
                    Console.WriteLine("ACCESSING jsonWeather ...");
                    client.Proxy = null;

                    string jsonWeather = client.DownloadString($"https://api.openweathermap.org/data/2.5/forecast?lat={latitude.ToString()}&lon={longitude.ToString()}&appid={Secret.OpenWeatherAPI}&units=metric");

                    Console.WriteLine(jsonWeather);

                    var myweather = JsonConvert.DeserializeObject<RootObject>(jsonWeather);

                    var currentWeather = myweather.list.ElementAt(0); // Current
                    var nextDayWeather = myweather.list.ElementAt(8); // 24 hours
                    var location = myweather.city.name + ", " + myweather.city.country;

                    float currentWindKnots = (float)(currentWeather.wind.speed * 1.9438452);
                    float nextDayWindKnots = (float)(nextDayWeather.wind.speed * 1.9438452);

                    ForeCast today = new ForeCast
                    {
                        weatherIcon = currentWeather.weather.ElementAt(0).icon,
                        temp = currentWeather.main.temp,
                        windKnots = currentWindKnots,
                        windDirection = windDegToDirection(currentWeather.wind.deg),

                    };

                    ForeCast tomorrow = new ForeCast
                    {
                        weatherIcon = nextDayWeather.weather.ElementAt(0).icon,
                        temp = nextDayWeather.main.temp,
                        windKnots = nextDayWindKnots,
                        windDirection = windDegToDirection(nextDayWeather.wind.deg),
                    };

                    WeatherData weatherData = new WeatherData
                    {
                        location = location,
                        today = today,
                        tomorrow = tomorrow,
                    };

                    var weatherJson = "WEATHER" + JsonConvert.SerializeObject(weatherData) + (char)0x03;
                    Console.WriteLine(weatherJson);
                    EnqueueData(weatherJson);
                }
            }
            catch (Exception)
            {

                Console.WriteLine("############################### ATTENTION: No internet connection for weather ###############################");
            }


        }
        /////////////////////////////////////////////////////////
        public async void tideData(object StateObj)
        {

            Console.WriteLine("Tide function entered");
            string[] times = new string[4];


            try
            {
                using (var client = new HttpClient())
                {
                    Console.WriteLine("ACCESSING tide information ...");

                    DateTime utcTimeNow = DateTime.UtcNow;
                    string isoFormat = utcTimeNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string urlEncoded = WebUtility.UrlEncode(isoFormat);

                    Console.WriteLine(urlEncoded);

                    client.DefaultRequestHeaders.Add("Authorization", Secret.StromGlassTideAPI);
                    string url = $"https://api.stormglass.io/v2/tide/extremes/point?lat={latitude.ToString()}&lng={longitude.ToString()}&start={urlEncoded}";

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonTide = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(jsonTide);

                        var myTides = JsonConvert.DeserializeObject<RootTideObject>(jsonTide);

                        int i = 0;
                        foreach (var json in myTides.data.Take(4))
                        {
                            //DateTime utcTime = DateTime.Parse(json.time);
                            //DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
                            DateTime utcTime = DateTime.SpecifyKind(
                                DateTime.Parse(json.time),
                                DateTimeKind.Utc);

                            DateTime localTime = utcTime.ToLocalTime();

                            string formattedTime = localTime.ToString("HH:mm");
                            times[i] = formattedTime;
                            i++;
                        }

                        TideData data = new TideData
                        {
                            Lo1 = times[0],
                            Hi1 = times[1],
                            Lo2 = times[2],
                            Hi2 = times[3],
                        };

                        var tideJson = "TIDE" + JsonConvert.SerializeObject(data) + (char)0x03;
                        Console.WriteLine(tideJson);
                        EnqueueData(tideJson);
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                    }

                    //string result = string.Join(", ", times);
                    //Console.WriteLine(result);

                    

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
            float[] coreNoLoad = new float[20];
            float CpuPower = -1.0f;
            float[] coreNoTemp = new float[20];
            float[] coreNoClock = new float[20];
            float RamUsed = -1.0f;
            float RamAvail = -1.0f;
            float UploadSpeed = -1.0f;
            float DownloadSpeed = -1.0f;
            float CpuFan = -1.0f;
            int numCores = 0;
            int cpuPackageTemp = 0;
            float gpuPower = -1.0f;
            float cpuPackagePower = -1.0f;

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
                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        if (s.Name.StartsWith("CPU Core #") && s.Name.Length == 11)
                    //        {

                    //            int coreid = int.Parse(s.Name.Split('#')[1]);
                    //            int coreIndex = coreid - 1;
                    //            numCores++;
                    //            //string corenumber = coreid.ToString();
                    //            //string coreNoTemp = "" + Convert.ToDouble(s.Value);

                    //            coreNoTemp[coreIndex] = (float)Convert.ToDouble(s.Value);

                    //            //Console.WriteLine(coreNoTempStr[coreid]);

                    //        }
                    //    }
                    //}

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature)
                    {
                        if (s.Value != null)
                        {
                            if (s.Name.StartsWith("CPU Package"))
                            {

                                //string corenumber = coreid.ToString();
                                //string coreNoTemp = "" + Convert.ToDouble(s.Value);

                                cpuPackageTemp = (int)Convert.ToDouble(s.Value);

                            }
                        }
                    }

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Power)
                    {
                        if (s.Value != null)
                        {
                            if (s.Name.StartsWith("CPU Package"))
                            {

                                //string corenumber = coreid.ToString();
                                //string coreNoTemp = "" + Convert.ToDouble(s.Value);

                                cpuPackagePower = (float)Convert.ToDouble(s.Value);

                            }
                        }
                    }

                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Fan)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float cpuFan = (float)Math.Round((double)s.Value, 2);
                    //        switch (s.Name)
                    //        {
                    //            case "GPU":
                    //                CpuFan = cpuFan;
                    //                break;
                    //        }
                    //    }
                    //}

                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Power)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float cpuPower = (float)Math.Round((double)s.Value, 2);
                    //        switch (s.Name)
                    //        {
                    //            case "CPU Package":
                    //                CpuPower = cpuPower;
                    //                break;
                    //        }
                    //    }
                    //}


                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        if (s.Name.StartsWith("CPU Core #") && s.Name.Length == 11)
                    //        {

                    //            int coreid = int.Parse(s.Name.Split('#')[1]);
                    //            int coreIndex = coreid - 1;

                    //            //string corenumber = coreid.ToString();
                    //            //string coreNoClock = "" + s.Value;

                    //            coreNoClock[coreIndex] = (float)s.Value;

                    //           // Console.WriteLine(coreNoClockStr[coreid]);

                    //        }
                    //    }
                    //}

                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        if (s.Name.StartsWith("CPU Core #") && s.Name.Length == 11)
                    //        {

                    //            int coreid = int.Parse(s.Name.Split('#')[1]);
                    //            int coreIndex = coreid - 1;

                    //            //string corenumber = coreid.ToString();
                    //            //string coreNoLoad = "" + Math.Round(Convert.ToDouble(s.Value),2);

                    //            coreNoLoad[coreIndex] = (float)Math.Round(Convert.ToDouble(s.Value), 2);

                    //           // Console.WriteLine(coreNoLoadStr[coreid]);

                    //        }
                    //    }
                    //}
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

                    if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Power)
                    {
                        if (s.Value != null)
                        {
                            if (s.Name.StartsWith("GPU Power"))
                            {

                                //string corenumber = coreid.ToString();
                                //string coreNoTemp = "" + Convert.ToDouble(s.Value);

                                gpuPower = (float)Convert.ToDouble(s.Value);

                            }
                        }
                    }

                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float gpuClock = (float)Math.Round((double)s.Value,2);
                    //        switch (s.Name)
                    //        {
                    //            case "GPU Core":
                    //                GpuClock = gpuClock;
                    //                break;
                    //        }
                    //    }
                    //}


                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float gpumemoryClock = (float)Math.Round((double)s.Value, 2);
                    //        switch (s.Name)
                    //        {
                    //            case "GPU Memory":
                    //                GpuMemoryClock = gpumemoryClock;
                    //                break;
                    //        }
                    //    }
                    //}

                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float gpuLoad = (float)Math.Round((double)s.Value, 2);
                    //        switch (s.Name)
                    //        {
                    //            case "GPU Core":
                    //                GpuLoad = gpuLoad;
                    //                break;
                    //        }
                    //    }
                    //}

                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Fan)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float gpuFan = (float)Math.Round((double)s.Value, 2);
                    //        switch (s.Name)
                    //        {
                    //            case "GPU":
                    //                GpuFan = gpuFan;
                    //                break;
                    //        }
                    //    }
                    //}

                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float gpuMemory = (float)Math.Round((double)s.Value, 2);
                    //        switch (s.Name)
                    //        {
                    //            case "GPU Memory":
                    //                GpuMemory = gpuMemory;
                    //                break;
                    //        }
                    //    }
                    //}


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
                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Throughput)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float uploadSpeed = (float)Math.Round((double)s.Value / 2600, 2);
                    //        switch (s.Name)
                    //        {
                    //            case "Upload Speed":
                    //                UploadSpeed = uploadSpeed;
                    //                break;
                    //        }
                    //    }
                    //}

                    //if (s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Throughput)
                    //{
                    //    if (s.Value != null)
                    //    {
                    //        float downloadSpeed = (float)Math.Round((double)s.Value / 2600, 2);
                    //        switch (s.Name)
                    //        {
                    //            case "Download Speed":
                    //                DownloadSpeed = downloadSpeed;
                    //                break;
                    //        }
                    //    }
                    //}
                }
            }

            ComputerData computerData = new ComputerData
            {
                RamUsed = RamUsed,
                RamAvail = RamAvail,
                GpuTemp = GpuTemp,
                CpuPackageTemp = cpuPackageTemp,
                CpuPackagePower = cpuPackagePower,
                GpuPower = gpuPower
            };

            var json = "HARDWARE" + JsonConvert.SerializeObject(computerData) + (char)0x03;

            Console.WriteLine(json);
            EnqueueData(json);
        }

        void Exit_Click(object sender, EventArgs e)
        {
            WinAmp.SendMessage(WinAmp.hwnd, WinAmp.WM_COMMAND, (IntPtr)WinAmp.TOGGLE_VISIBILITY, IntPtr.Zero);
            WinAmp.SendMessage(WinAmp.hwnd, WinAmp.WM_COMMAND, (IntPtr)WinAmp.CLOSE_WINAMP, IntPtr.Zero);

            Application.Exit();
        }
    }
}