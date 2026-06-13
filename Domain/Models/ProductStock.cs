using Domain.Common;
using Domain.Exceptions;

namespace Domain.Models
{
    public class ProductStock : BaseEntity
    {
        public Guid ProductId { get; private set; }
        public Product? Product { get; private set; }
        public int Quantity { get; private set; }
        public int Reserved { get; private set; }
        public string Warehouse { get; private set; }
        public int LeadTimeDays { get; private set; }

        public int AvailableQuantity => Quantity - Reserved;
        public bool IsInStock => AvailableQuantity > 0;

        private ProductStock() { }

        public ProductStock(Guid productId, int quantity, string warehouse = "main", int leadTimeDays = 3)
        {
            if (quantity < 0)
                throw new DomainException("Quantity cannot be negative");

            ProductId = productId;
            Quantity = quantity;
            Reserved = 0;
            Warehouse = warehouse ?? throw new DomainException("Warehouse is required");
            LeadTimeDays = leadTimeDays;
        }

        public bool TryReserve(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Reserve quantity must be positive");

            if (AvailableQuantity >= quantity)
            {
                Reserved += quantity;
                UpdateTimestamp();
                return true;
            }

            return false;
        }

        public void CommitReservation(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Commit quantity must be positive");

            if (Reserved < quantity)
                throw new DomainException($"Cannot commit {quantity} units, only {Reserved} reserved");

            Quantity -= quantity;
            Reserved -= quantity;
            UpdateTimestamp();
        }

        public void CancelReservation(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Cancel quantity must be positive");

            if (Reserved < quantity)
                throw new DomainException($"Cannot cancel {quantity} units, only {Reserved} reserved");

            Reserved -= quantity;
            UpdateTimestamp();
        }

        public void AddStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Add stock quantity must be positive");

            Quantity += quantity;
            UpdateTimestamp();
        }

        public void RemoveStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Remove stock quantity must be positive");

            if (Quantity - Reserved < quantity)
                throw new DomainException($"Insufficient available stock. Available: {AvailableQuantity}");

            Quantity -= quantity;
            UpdateTimestamp();
        }

        public bool IsAvailable() => IsInStock;
    }
}
