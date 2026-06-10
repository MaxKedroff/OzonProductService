namespace Application.Events
{
    public class OrderPaidEvent
    {
        public Guid OrderId { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public DateTime PaidAt { get; set; }
    }

    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtPurchase { get; set; }
    }
}
