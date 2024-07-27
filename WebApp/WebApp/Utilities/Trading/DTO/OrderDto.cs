using Bd.Enums;

namespace WebApp.Utilities.Trading.DTO
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Symbol { get; set; }
        public float Quantity { get; set; }
        public float Price { get; set; }
        public float? ClosedPrice { get; set; }
        public OrderType OrderType { get; set; }
        public OrderState OrderState { get; set; } // New property
        public float? StopLoss { get; set; } // New property
        public float? TakeProfit { get; set; } // New property
        public float? ProfitorLoss { get; set; } // New property
    }


}
