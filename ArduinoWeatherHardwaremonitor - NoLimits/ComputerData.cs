using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSender
{
    public struct ComputerData
    {
        public string DownloadSpeed;
        public string UploadSpeed;
        public string RamUsed;
        public string RamAvail;
        public string GpuLoad;
        public string GpuFan;
        public string GpuMemory;
        public string GpuMemoryClock;
        public string GpuClock;
        public string GpuTemp;
        public string CpuFan;
        public string CpuPower;
        public string[] CoreNoTemp;
        public string[] CoreNoClock;
        public string[] CoreNoLoad;
    }
}
