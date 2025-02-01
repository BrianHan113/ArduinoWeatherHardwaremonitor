namespace SerialSender
{
    public struct TideForecast
    {
        public TideData High1;
        public TideData Low;
        public TideData High2;
        public string currentTime;
    }

    public struct TideData
    {
        public string time;
        public float height;
    }
}
