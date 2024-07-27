namespace WebApp.Utilities.Trading.DTO
{
    public class StockDataDto
    {
        public string? Symbol { get; set; }
        public long TimeStamp { get; set; }
        public float Open { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
        public float Close { get; set; }
        public string? Interval { get; set; }
    }

}
