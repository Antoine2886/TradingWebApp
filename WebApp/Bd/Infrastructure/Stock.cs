using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bd.Enums;
namespace Bd.Infrastructure
{
    /// <summary>
    /// Basic implementation of the stock class (test)
    /// </summary>
    public class Stock
    {
        public int Id { get; set; }

        public StockType StockType { get; set; }
        public float Leverage { get; set; } 
        public string? Name { get; set; }

        public ICollection<StockData> StockData { get; set; }
        public float Bid { get; set; }
        public float Ask { get; set; }



    }


    public class StockData
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Interval { get; set; } // Property to distinguish between intervals
        public float Open { get; set; }
        public float Low { get; set; }
        public float Close { get; set; }
        public float High { get; set; }
        public virtual Stock Stock { get; set; } // Navigation property
    }


}
