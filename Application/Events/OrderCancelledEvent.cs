namespace Application.Events
{
    public class OrderCancelledEvent
    {
        public Guid OrderId { get; set; }
        public List<OrderCancelledItem> Items { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
        public DateTime CancelledAt { get; set; }
    }

    public class OrderCancelledItem
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
