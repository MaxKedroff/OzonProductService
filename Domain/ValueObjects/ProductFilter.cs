namespace Domain.ValueObjects
{
    public class ProductFilter
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStockOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "createdAt";
        public bool SortDescending { get; set; } = true;

        public int Offset => (Page - 1) * PageSize;

        public bool HasSearch => !string.IsNullOrWhiteSpace(SearchTerm);
        public bool HasPriceRange => MinPrice.HasValue || MaxPrice.HasValue;
        public bool HasCategory => !string.IsNullOrWhiteSpace(Category);
        public bool HasStockFilter => InStockOnly.HasValue;
    }
}
