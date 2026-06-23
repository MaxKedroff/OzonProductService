using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;
namespace Tests.Unit.Domain.ValueObjects
{
    [TestFixture]
    public class MoneyTests
    {
        [Test]
        public void ConstructorWithValidAmountShouldCreateMoney()
        {
            var money = new Money(99.99m, "USD");

            money.Amount.Should().Be(99.99m);
            money.Currency.Should().Be("USD");
        }

        [Test]
        public void ConstructorWithNegativeAmountShouldThrowDomainException()
        {
            Action act = () => new Money(-10m, "USD");

            act.Should().Throw<DomainException>()
                .WithMessage("Amount cannot be negative");
        }

        [Test]
        public void ConstructorWithEmptyCurrencyShouldThrowDomainException()
        {
            Action act = () => new Money(100m, "");

            act.Should().Throw<DomainException>()
                .WithMessage("Currency is required");
        }

        [Test]
        public void AddWithSameCurrencyShouldReturnSum()
        {
            var money1 = new Money(100m, "USD");
            var money2 = new Money(50m, "USD");

            var result = money1.Add(money2);

            result.Amount.Should().Be(150m);
            result.Currency.Should().Be("USD");
        }

        [Test]
        public void AddWithDifferentCurrencyShouldThrowDomainException()
        {
            var money1 = new Money(100m, "USD");
            var money2 = new Money(50m, "EUR");

            Action act = () => money1.Add(money2);

            act.Should().Throw<DomainException>()
                .WithMessage("Cannot add different currencies: USD and EUR");
        }

        [Test]
        public void SubtractWithValidAmountShouldReturnDifference()
        {
            var money1 = new Money(100m, "USD");
            var money2 = new Money(30m, "USD");

            var result = money1.Subtract(money2);

            result.Amount.Should().Be(70m);
        }

        [Test]
        public void SubtractWithInsufficientFundsShouldThrowDomainException()
        {
            var money1 = new Money(100m, "USD");
            var money2 = new Money(150m, "USD");

            Action act = () => money1.Subtract(money2);

            act.Should().Throw<DomainException>()
                .WithMessage("Insufficient funds");
        }

        [Test]
        public void EqualsWithDifferentAmountShouldReturnFalse()
        {
            var money1 = new Money(100m, "USD");
            var money2 = new Money(200m, "USD");

            money1.Equals(money2).Should().BeFalse();
        }

        [Test]
        public void ToStringShouldReturnFormattedString()
        {
            var money = new Money(99.99m, "USD");

            var result = money.ToString();

            result.Should().Be("99,99 USD");
        }
    }
}