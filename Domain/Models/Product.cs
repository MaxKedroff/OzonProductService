using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Models
{
    public class Product : BaseEntity
    {
        private string _name = string.Empty;
        private string _description = string.Empty;

        public string Name
        {
            get => _name;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException("Product name cannot be empty");

                if (value.Length > 200)
                    throw new DomainException("Product name cannot exceed 200 characters");

                _name = value;
            }
        }

        public string Description
        {
            get => _description;
            private set => _description = value ?? string.Empty;
        }

        public Money Price { get; private set; }
        public Sku Sku { get; private set; }
        public ProductCategory Category { get; private set; }
        public bool IsDeleted { get; private set; }
        public ProductStock? Stock { get; private set; }

        private Product() { }

        public Product(string name, string description, Money price, Sku sku, ProductCategory category)
        {
            Name = name;
            Description = description;
            Price = price ?? throw new DomainException("Price is required");
            Sku = sku ?? throw new DomainException("SKU is required");
            Category = category;
            IsDeleted = false;
        }

        public void UpdateDetails(string name, string description, ProductCategory category)
        {
            Name = name;
            Description = description;
            Category = category;
            UpdateTimestamp();
        }

        public void UpdatePrice(Money newPrice)
        {
            Price = newPrice ?? throw new DomainException("Price cannot be null");
            UpdateTimestamp();
        }

        public void UpdateSku(Sku newSku)
        {
            Sku = newSku ?? throw new DomainException("SKU cannot be null");
            UpdateTimestamp();
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            UpdateTimestamp();
        }

        public void Restore()
        {
            IsDeleted = false;
            UpdateTimestamp();
        }

        public void SetStock(ProductStock stock)
        {
            Stock = stock;
        }

        public bool IsAvailable() => Stock != null && Stock.IsAvailable();
    }
}
