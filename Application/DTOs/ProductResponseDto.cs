namespace Application.DTOs
{
    public class ProductResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public string Sku { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int AvailableStock { get; set; }
        public bool IsInStock { get; set; }
        public int LeadTimeDays { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
