using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ProductBatchDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int AvailableStock { get; set; }
        public bool IsAvailable { get; set; }
        public int LeadTimeDays { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Sku { get; set; }
    }
}
