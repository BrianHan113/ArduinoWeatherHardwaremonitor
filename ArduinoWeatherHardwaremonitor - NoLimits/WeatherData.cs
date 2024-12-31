using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSender
{
    public struct WeatherData
    {
        public ForeCast currentForecast;
        public ForeCast threeHourForecast;
        public ForeCast sixHourForecast;
        public ForeCast nineHourForecast;
        public ForeCast twelveHourForecast;
        public ForeCast fifteenHourForecast;
        public ForeCast eighteenHourForecast;
        public ForeCast twentyOneHourForecast;
        public ForeCast twentyFourHourForecast;
    }

    public struct ForeCast
    {
        public String description;
        public float temperature;
        public float windSpeed;
        public int humidity;
    }
}
