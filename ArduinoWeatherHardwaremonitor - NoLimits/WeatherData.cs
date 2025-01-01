using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSender
{
    public struct WeatherData
    {
        public String timeSpan;
        public ForeCast forecast1;
        public ForeCast forecast2;
        public ForeCast forecast3;
    }

    public struct ForeCast
    {
        public String desc;
        public float temp;
        public float wind;
        public int humid;
    }
}
