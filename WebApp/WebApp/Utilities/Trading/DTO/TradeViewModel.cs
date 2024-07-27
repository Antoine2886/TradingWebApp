using Bd.Infrastructure;

namespace WebApp.Utilities.Trading.DTO
{
    public class TradeViewModel
    {
        public string Symbol { get; set; }
        public string Interval { get; set; }
        public string ChartType { get; set; }
        public string TimeZone { get; set; }
        public string Theme { get; set; }
        public string LineColor { get; set; }
        public string UpColor { get; set; }
        public string DownColor { get; set; }
        public List<Drawing> Drawings { get; set; }
    }
}
