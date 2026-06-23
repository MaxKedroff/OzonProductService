using Domain.Enums;
using Domain.Models;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Repositories;

namespace Tests.Integration
{
    [TestFixture]
    public class StockRepositoryTests : IntegrationTestBase
    {
        private StockRepository _repository = null!;
        private ProductRepository _productRepository = null!;

        [SetUp]
        public new async Task SetUp()
        {
            await base.SetUp();
            _repository = new StockRepository(_connectionFactory);
            _productRepository = new ProductRepository(_connectionFactory);
        }

        private async Task<Product> CreateTestProduct(Guid? id = null)
        {
            var productId = id ?? Guid.NewGuid();
            var skuValue = $"TEST-{Guid.NewGuid():N}".ToUpperInvariant();

            var product = new Product(
                "Test Product",
                "Test Description",
                new Money(99.99m, "USD"),
                new Sku(skuValue),
                ProductCategory.Electronics
            );

            var productType = product.GetType();
            productType.GetProperty("Id")?.SetValue(product, productId);
            productType.GetProperty("CreatedAt")?.SetValue(product, DateTime.UtcNow);

            await _productRepository.AddAsync(product);
            return product;
        }

        [Test]
        public async Task AddAsync_ShouldInsertStock()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10, "main", 3);

            await _repository.AddAsync(stock);
            var result = await _repository.GetByProductIdAsync(product.Id);

            result.Should().NotBeNull();
            result!.ProductId.Should().Be(product.Id);
            result.Quantity.Should().Be(10);
            result.Reserved.Should().Be(0);
            result.Warehouse.Should().Be("main");
            result.LeadTimeDays.Should().Be(3);
            result.IsInStock.Should().BeTrue();
            result.AvailableQuantity.Should().Be(10);
        }

        [Test]
        public async Task GetByProductIdAsyncWhenStockExistsShouldReturnStock()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10);
            await _repository.AddAsync(stock);

            var result = await _repository.GetByProductIdAsync(product.Id);

            result.Should().NotBeNull();
            result!.ProductId.Should().Be(product.Id);
            result.Quantity.Should().Be(10);
        }

        [Test]
        public async Task GetByProductIdAsync_WhenStockNotExists_ShouldReturnNull()
        {
            var result = await _repository.GetByProductIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Test]
        public async Task GetByProductIdsAsync_ShouldReturnMultipleStocks()
        {
            var product1 = await CreateTestProduct();
            var product2 = await CreateTestProduct();

            var stock1 = new ProductStock(product1.Id, 10);
            var stock2 = new ProductStock(product2.Id, 20);

            await _repository.AddAsync(stock1);
            await _repository.AddAsync(stock2);

            var results = await _repository.GetByProductIdsAsync(new[] { product1.Id, product2.Id });

            results.Should().HaveCount(2);
            results.Select(s => s.ProductId).Should().Contain(product1.Id);
            results.Select(s => s.ProductId).Should().Contain(product2.Id);
        }

        [Test]
        public async Task UpdateAsyncShouldUpdateStock()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10);
            await _repository.AddAsync(stock);

            stock.AddStock(5);
            stock.TryReserve(3);
            await _repository.UpdateAsync(stock);

            var result = await _repository.GetByProductIdAsync(product.Id);

            result.Should().NotBeNull();
            result!.Quantity.Should().Be(15);
            result.Reserved.Should().Be(3);
            result.AvailableQuantity.Should().Be(12);
            result.UpdatedAt.Should().NotBeNull();
        }

        [Test]
        public async Task TryReserveStockAsyncWithSufficientStockShouldReturnTrue()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10);
            await _repository.AddAsync(stock);

            var result = await _repository.TryReserveStockAsync(product.Id, 3);
            var updatedStock = await _repository.GetByProductIdAsync(product.Id);

            result.Should().BeTrue();
            updatedStock!.Reserved.Should().Be(3);
            updatedStock.AvailableQuantity.Should().Be(7);
            updatedStock.UpdatedAt.Should().NotBeNull();
        }

        [Test]
        public async Task TryReserveStockAsyncWithInsufficientStockShouldReturnFalse()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 5);
            await _repository.AddAsync(stock);

            var result = await _repository.TryReserveStockAsync(product.Id, 10);
            var updatedStock = await _repository.GetByProductIdAsync(product.Id);

            result.Should().BeFalse();
            updatedStock!.Reserved.Should().Be(0);
            updatedStock.AvailableQuantity.Should().Be(5);
        }

        [Test]
        public async Task CommitReservationAsync_ShouldCommitReservation()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10);
            await _repository.AddAsync(stock);
            await _repository.TryReserveStockAsync(product.Id, 3);

            var result = await _repository.CommitReservationAsync(product.Id, 3);
            var updatedStock = await _repository.GetByProductIdAsync(product.Id);

            result.Should().BeTrue();
            updatedStock!.Quantity.Should().Be(7);
            updatedStock.Reserved.Should().Be(0);
            updatedStock.AvailableQuantity.Should().Be(7);
        }

        [Test]
        public async Task CommitReservationAsyncWithMoreThanReservedShouldReturnFalse()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10);
            await _repository.AddAsync(stock);
            await _repository.TryReserveStockAsync(product.Id, 3);

            var result = await _repository.CommitReservationAsync(product.Id, 5);
            var updatedStock = await _repository.GetByProductIdAsync(product.Id);

            result.Should().BeFalse();
            updatedStock!.Quantity.Should().Be(10);
            updatedStock.Reserved.Should().Be(3);
        }

        [Test]
        public async Task CancelReservationAsyncShouldCancelReservation()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10);
            await _repository.AddAsync(stock);
            await _repository.TryReserveStockAsync(product.Id, 5);

            var result = await _repository.CancelReservationAsync(product.Id, 3);
            var updatedStock = await _repository.GetByProductIdAsync(product.Id);

            result.Should().BeTrue();
            updatedStock!.Reserved.Should().Be(2);
            updatedStock.AvailableQuantity.Should().Be(8);
        }

        [Test]
        public async Task CancelReservationAsync_WithMoreThanReserved_ShouldReturnFalse()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10);
            await _repository.AddAsync(stock);
            await _repository.TryReserveStockAsync(product.Id, 3);

            var result = await _repository.CancelReservationAsync(product.Id, 5);
            var updatedStock = await _repository.GetByProductIdAsync(product.Id);

            result.Should().BeFalse();
            updatedStock!.Reserved.Should().Be(3);
        }

        [Test]
        public async Task ConcurrentReservationsShouldBeConsistent()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 10);
            await _repository.AddAsync(stock);

            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_repository.TryReserveStockAsync(product.Id, 2));
            }
            await Task.WhenAll(tasks);

            var updatedStock = await _repository.GetByProductIdAsync(product.Id);

            updatedStock!.Reserved.Should().Be(10);
            updatedStock.AvailableQuantity.Should().Be(0);
        }

        [Test]
        public async Task CommitReservation_WithConcurrentOperations_ShouldBeConsistent()
        {
            var product = await CreateTestProduct();
            var stock = new ProductStock(product.Id, 20);
            await _repository.AddAsync(stock);

            await _repository.TryReserveStockAsync(product.Id, 15);

            var tasks = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(_repository.CommitReservationAsync(product.Id, 5));
            }
            await Task.WhenAll(tasks);

            var updatedStock = await _repository.GetByProductIdAsync(product.Id);

            updatedStock!.Quantity.Should().Be(5);
            updatedStock.Reserved.Should().Be(0);
            updatedStock.AvailableQuantity.Should().Be(5);
        }

        [Test]
        public async Task TryReserveStockAsyncWhenStockNotExistsShouldReturnFalse()
        {
            var productId = Guid.NewGuid();

            var result = await _repository.TryReserveStockAsync(productId, 5);

            result.Should().BeFalse();
        }

        [Test]
        public async Task GetByProductIdsAsyncWithEmptyListShouldReturnEmpty()
        {
            var result = await _repository.GetByProductIdsAsync(new List<Guid>());

            result.Should().BeEmpty();
        }


    }
}
