using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;

namespace Tests.Domain.ValueObjects
{
    [TestFixture]
    public class SkuTests
    {
        [Test]
        public void ConstructorWithValidSkuShouldCreateSku()
        {
            var sku = new Sku("PROD-001");

            sku.Value.Should().Be("PROD-001");
        }

        [Test]
        public void ConstructorWithEmptySkuShouldThrowDomainException()
        {
            Action act = () => new Sku("");

            act.Should().Throw<DomainException>()
                .WithMessage("SKU cannot be empty");
        }

        [Test]
        public void ConstructorWithInvalidCharactersShouldThrowDomainException()
        {
            Action act = () => new Sku("PROD_001");

            act.Should().Throw<DomainException>()
                .WithMessage("SKU must contain only uppercase letters, numbers, and hyphens");
        }

        [Test]
        public void ConstructorWithTooLongSkuShouldThrowDomainException()
        {
            var longSku = new string('A', 51);

            Action act = () => new Sku(longSku);

            act.Should().Throw<DomainException>()
                .WithMessage("SKU cannot exceed 50 characters");
        }

        [Test]
        public void EqualsWithSameSkuShouldReturnTrue()
        {
            var sku1 = new Sku("PROD-001");
            var sku2 = new Sku("PROD-001");

            sku1.Equals(sku2).Should().BeTrue();
        }

        [Test]
        public void ToStringShouldReturnValue()
        {
            var sku = new Sku("PROD-001");

            var result = sku.ToString();

            result.Should().Be("PROD-001");
        }
    }
}
