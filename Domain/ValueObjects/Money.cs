using Domain.Exceptions;

namespace Domain.ValueObjects
{
    public class Money : IEquatable<Money>
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency = "USD")
        {
            if (amount < 0)
                throw new DomainException("Amount cannot be negative");

            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("Currency is required");

            Amount = Math.Round(amount, 2);
            Currency = currency.ToUpperInvariant();
        }

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new DomainException($"Cannot add different currencies: {Currency} and {other.Currency}");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new DomainException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

            if (Amount < other.Amount)
                throw new DomainException("Insufficient funds");

            return new Money(Amount - other.Amount, Currency);
        }

        public override bool Equals(object? obj) => Equals(obj as Money);

        public bool Equals(Money? other)
        {
            if (other is null) return false;
            return Amount == other.Amount && Currency == other.Currency;
        }

        public override int GetHashCode() => HashCode.Combine(Amount, Currency);

        public override string ToString() => $"{Amount} {Currency}";
    }
}
