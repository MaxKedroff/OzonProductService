using Domain.Exceptions;

namespace Domain.ValueObjects
{
    public class Sku : IEquatable<Sku>
    {
        public string Value { get; }

        public Sku(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("SKU cannot be empty");

            if (value.Length > 50)
                throw new DomainException("SKU cannot exceed 50 characters");

            if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Z0-9\-]+$"))
                throw new DomainException("SKU must contain only uppercase letters, numbers, and hyphens");

            Value = value;
        }

        public override bool Equals(object? obj) => Equals(obj as Sku);

        public bool Equals(Sku? other)
        {
            if (other is null) return false;
            return Value == other.Value;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value;
    }
}
