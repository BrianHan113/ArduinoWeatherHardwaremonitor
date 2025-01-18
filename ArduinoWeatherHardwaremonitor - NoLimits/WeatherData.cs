using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSender
{
    public struct WeatherData
    {
        public ForeCast today;
        public ForeCast tomorrow;
    }

    public struct ForeCast
    {
        public String weatherIcon;
        public float temp;
        public float windKnots;
        public String windDirection;
    }
}
