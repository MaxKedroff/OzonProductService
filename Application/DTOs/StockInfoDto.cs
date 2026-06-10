namespace Application.DTOs
{
    public class StockInfoDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public int Reserved { get; set; }
        public int Available { get; set; }
        public string Warehouse { get; set; } = string.Empty;
        public int LeadTimeDays { get; set; }
        public bool IsInStock { get; set; }
    }
}
