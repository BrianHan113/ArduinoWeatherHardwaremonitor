using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSender
{
    public struct WeatherData
    {
        public String location;
        public ForeCast forecast1;
        public ForeCast forecast2;
        public ForeCast forecast3;
        public ForeCast forecast4;
    }

    public struct ForeCast
    {
        public int id;
        public float temp;
        public int prec_probability;
        public float prec;
        public int weather_code;
        public float wind_speed;
        public int wind_dir;
        public bool isDay;
    }

    public struct Location
    {
        public double latitude;
        public double longitude;
    }
}
