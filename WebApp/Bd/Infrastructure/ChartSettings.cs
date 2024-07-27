using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bd.Infrastructure
{
    public class ChartSettings
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string Symbol { get; set; }
        public string Interval { get; set; }
        public string ChartType { get; set; }
        public string TimeZone { get; set; }
        public string Theme { get; set; }
        public string LineColor { get; set; }
        public string UpColor { get; set; }
        public string DownColor { get; set; }

        [JsonIgnore]
        public List<Drawing> Drawings { get; set; } = new List<Drawing>();
    }

    public class Drawing
    {
        public Guid Id { get; set; }

        [JsonIgnore]
        public ChartSettings ChartSettings { get; set; }
        public string Type { get; set; }
        public string Coordinates { get; set; }
    }

}
