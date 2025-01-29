using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSender
{
    public struct TideForecast
    {
        public TideData High1;
        public TideData Low;
        public TideData High2;
    }

    public struct TideData
    {
        public string time;
        public float height;
    }
}
