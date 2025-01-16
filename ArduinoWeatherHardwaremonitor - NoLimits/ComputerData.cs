using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSender
{
    public struct ComputerData
    {
        //public float DownloadSpeed;
        //public float UploadSpeed;
        public float RamUsed;
        public float RamAvail;
        //public float GpuLoad;
        //public float GpuFan;
        //public float GpuMemory;
        //public float GpuMemoryClock;
        //public float GpuClock;
        public float GpuTemp;
        //public float CpuFan;
        //public float CpuPower;
        public float[] CoreNoTemp;
        //public float[] CoreNoClock;
        //public float[] CoreNoLoad;
        public int NumCores;
    }
}
