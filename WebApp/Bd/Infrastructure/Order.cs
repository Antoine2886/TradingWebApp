using Bd.Enums;
using IdentityCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bd.Infrastructure
{
    public class Order
    {
        // add profit
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public float QuantityInLots { get; set; } // Quantity in lots
        public Stock Stock { get; set; }
        public AppUser User { get; set; }
        public float Quantity { get; set; }
        public float Price { get; set; }
        public float ProfitOrLoss { get; set; }
        public float Margin { get; set; }
        public float InitialCost { get; set; }
        public float ClosedPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public OrderType OrderType { get; set; }
        public OrderState OrderState { get; set; }
        public float? StopLoss { get; set; }
        public float? TakeProfit { get; set; }
        public const float LotSize = 100000; // 1 lot = 100,000 units
        public float QuantityInUnits => QuantityInLots * LotSize; // Quantity in units based on lots

    }
}
