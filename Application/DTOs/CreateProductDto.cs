namespace Application.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public string Sku { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int InitialStock { get; set; } = 0;
        public string Warehouse { get; set; } = "main";
        public int LeadTimeDays { get; set; } = 3;
    }
}
