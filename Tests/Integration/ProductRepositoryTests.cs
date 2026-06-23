using Dapper;
using Domain.Enums;
using Domain.Models;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Repositories;

namespace Tests.Integration
{
    public class ProductRepositoryTests : IntegrationTestBase
    {
        private ProductRepository _repository = null!;

        [SetUp]
        public new async Task SetUp()
        {
            await base.SetUp();
            _repository = new ProductRepository(_connectionFactory);
        }

        private Product CreateTestProduct(Guid? id = null)
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

            return product;
        }

        [Test]
        public async Task AddAsyncShouldInsertProduct()
        {
            var product = CreateTestProduct();

            await _repository.AddAsync(product);
            var result = await _repository.GetByIdAsync(product.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(product.Id);
            result.Name.Should().Be("Test Product");
            result.Price.Amount.Should().Be(99.99m);
            result.Price.Currency.Should().Be("USD");
            result.Sku.Value.Should().Be(product.Sku.Value);
            result.Category.Should().Be(ProductCategory.Electronics);
            result.IsDeleted.Should().BeFalse();
        }

        [Test]
        public async Task GetByIdAsyncWhenProductExistsShouldReturnProduct()
        {
            var product = CreateTestProduct();
            await _repository.AddAsync(product);

            var result = await _repository.GetByIdAsync(product.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(product.Id);
            result.Name.Should().Be(product.Name);
        }

        [Test]
        public async Task GetByIdAsync_WhenProductNotExists_ShouldReturnNull()
        {
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Test]
        public async Task GetBySkuAsync_WhenProductExists_ShouldReturnProduct()
        {
            var product = CreateTestProduct();
            await _repository.AddAsync(product);

            var result = await _repository.GetBySkuAsync(product.Sku.Value);

            result.Should().NotBeNull();
            result!.Id.Should().Be(product.Id);
            result.Sku.Value.Should().Be(product.Sku.Value);
        }

        [Test]
        public async Task GetBySkuAsync_WhenProductNotExists_ShouldReturnNull()
        {
            var result = await _repository.GetBySkuAsync("NON-EXISTENT");

            result.Should().BeNull();
        }

        [Test]
        public async Task GetByIdsAsync_ShouldReturnMultipleProducts()
        {
            var product1 = CreateTestProduct();
            var product2 = CreateTestProduct();
            await _repository.AddAsync(product1);
            await _repository.AddAsync(product2);

            var ids = new[] { product1.Id, product2.Id };

            var results = await _repository.GetByIdsAsync(ids);

            results.Should().HaveCount(2);
            results.Select(p => p.Id).Should().Contain(product1.Id);
            results.Select(p => p.Id).Should().Contain(product2.Id);
        }

        [Test]
        public async Task UpdateAsyncShouldUpdateProduct()
        {
            var product = CreateTestProduct();
            await _repository.AddAsync(product);

            product.UpdateDetails("Updated Name", "Updated Description", ProductCategory.Clothing);
            product.UpdatePrice(new Money(199.99m, "USD"));

            await _repository.UpdateAsync(product);
            var result = await _repository.GetByIdAsync(product.Id);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Updated Name");
            result.Description.Should().Be("Updated Description");
            result.Category.Should().Be(ProductCategory.Clothing);
            result.Price.Amount.Should().Be(199.99m);
            result.UpdatedAt.Should().NotBeNull();
        }

        [Test]
        public async Task DeleteAsync_ShouldSoftDeleteProduct()
        {
            var product = CreateTestProduct();
            await _repository.AddAsync(product);

            var existsBefore = await _repository.ExistsAsync(product.Id);
            existsBefore.Should().BeTrue();

            await _repository.DeleteAsync(product.Id);

            var existsAfter = await _repository.ExistsAsync(product.Id);
            existsAfter.Should().BeFalse();

            var connection = _connectionFactory.CreateConnection();
            var deletedProduct = await connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT is_deleted FROM products WHERE id = @Id",
                new { Id = product.Id }
            );

            if (deletedProduct == null)
            {
                Assert.Fail("Product record not found in database after soft delete. It might have been physically deleted.");
            }

            bool isDeleted = (bool)deletedProduct.is_deleted;
            isDeleted.Should().BeTrue();
        }

        [Test]
        public async Task ExistsAsyncShouldReturnTrueIfProductExists()
        {
            var product = CreateTestProduct();
            await _repository.AddAsync(product);

            var exists = await _repository.ExistsAsync(product.Id);

            exists.Should().BeTrue();
        }

        [Test]
        public async Task ExistsAsyncShouldReturnFalseIfProductNotExists()
        {
            var exists = await _repository.ExistsAsync(Guid.NewGuid());

            exists.Should().BeFalse();
        }

        [Test]
        public async Task ExistsBySkuAsyncShouldReturnTrueIfSkuExists()
        {
            var product = CreateTestProduct();
            await _repository.AddAsync(product);

            var exists = await _repository.ExistsBySkuAsync(product.Sku.Value);

            exists.Should().BeTrue();
        }

        [Test]
        public async Task GetFilteredAsyncWithSearchTermShouldReturnMatchingProducts()
        {
            var product1 = CreateTestProduct();
            var product2 = CreateTestProduct();
            var product3 = CreateTestProduct();

            product1.UpdateDetails("iPhone 15", "Smartphone", ProductCategory.Electronics);
            product2.UpdateDetails("MacBook Pro", "Laptop", ProductCategory.Electronics);
            product3.UpdateDetails("Galaxy S24", "Smartphone", ProductCategory.Electronics);

            await _repository.AddAsync(product1);
            await _repository.AddAsync(product2);
            await _repository.AddAsync(product3);

            var filter = new ProductFilter
            {
                SearchTerm = "iPhone",
                Page = 1,
                PageSize = 10
            };

            var results = await _repository.GetFilteredAsync(filter);
            var totalCount = await _repository.GetTotalCountAsync(filter);

            results.Should().HaveCount(1);
            results.First().Name.Should().Contain("iPhone");
            totalCount.Should().Be(1);
        }

        [Test]
        public async Task GetFilteredAsyncWithCategoryFilterShouldReturnCorrectProducts()
        {
            var product1 = CreateTestProduct();
            var product2 = CreateTestProduct();

            product1.UpdateDetails("Product1", "Desc", ProductCategory.Electronics);
            product2.UpdateDetails("Product2", "Desc", ProductCategory.Clothing);

            await _repository.AddAsync(product1);
            await _repository.AddAsync(product2);

            var filter = new ProductFilter
            {
                Category = "Electronics",
                Page = 1,
                PageSize = 10
            };

            var results = await _repository.GetFilteredAsync(filter);
            var totalCount = await _repository.GetTotalCountAsync(filter);

            results.Should().HaveCount(1);
            results.First().Category.Should().Be(ProductCategory.Electronics);
            totalCount.Should().Be(1);
        }

        [Test]
        public async Task GetFilteredAsyncWithPriceRangeShouldReturnCorrectProducts()
        {
            var product1 = CreateTestProduct();
            var product2 = CreateTestProduct();
            var product3 = CreateTestProduct();

            product1.UpdatePrice(new Money(50m, "USD"));
            product2.UpdatePrice(new Money(150m, "USD"));
            product3.UpdatePrice(new Money(250m, "USD"));

            await _repository.AddAsync(product1);
            await _repository.AddAsync(product2);
            await _repository.AddAsync(product3);

            var filter = new ProductFilter
            {
                MinPrice = 100,
                MaxPrice = 200,
                Page = 1,
                PageSize = 10
            };

            var results = await _repository.GetFilteredAsync(filter);
            var totalCount = await _repository.GetTotalCountAsync(filter);

            results.Should().HaveCount(1);
            results.First().Price.Amount.Should().Be(150m);
            totalCount.Should().Be(1);
        }

        [Test]
        public async Task GetFilteredAsyncWithPaginationShouldReturnCorrectPage()
        {
            for (int i = 0; i < 15; i++)
            {
                var product = CreateTestProduct();
                await _repository.AddAsync(product);
            }

            var filter = new ProductFilter
            {
                Page = 2,
                PageSize = 5,
                SortBy = "createdat",
                SortDescending = true
            };

            var results = await _repository.GetFilteredAsync(filter);
            var totalCount = await _repository.GetTotalCountAsync(filter);

            results.Should().HaveCount(5);
            totalCount.Should().Be(15);
        }

        [Test]
        public async Task GetFilteredAsyncWithSortingShouldReturnSortedProducts()
        {
            var product1 = CreateTestProduct();
            var product2 = CreateTestProduct();
            var product3 = CreateTestProduct();

            product1.UpdatePrice(new Money(300m, "USD"));
            product2.UpdatePrice(new Money(100m, "USD"));
            product3.UpdatePrice(new Money(200m, "USD"));

            await _repository.AddAsync(product1);
            await _repository.AddAsync(product2);
            await _repository.AddAsync(product3);

            var filter = new ProductFilter
            {
                SortBy = "price",
                SortDescending = false,
                Page = 1,
                PageSize = 10
            };

            var results = await _repository.GetFilteredAsync(filter);

            var prices = results.Select(p => p.Price.Amount).ToList();
            prices.Should().BeInAscendingOrder();
            prices.Should().ContainInOrder(100m, 200m, 300m);
        }

        [Test]
        public async Task GetFilteredAsyncWithCombinedFiltersShouldReturnCorrectProducts()
        {
            var product1 = CreateTestProduct();
            var product2 = CreateTestProduct();
            var product3 = CreateTestProduct();

            product1.UpdateDetails("Apple iPhone 15 Pro", "Smartphone with A17 chip", ProductCategory.Electronics);
            product1.UpdatePrice(new Money(999.99m, "USD"));

            product2.UpdateDetails("Samsung Galaxy S24 Ultra", "Android flagship", ProductCategory.Electronics);
            product2.UpdatePrice(new Money(899.99m, "USD"));

            product3.UpdateDetails("Apple MacBook Pro", "Professional laptop", ProductCategory.Electronics);
            product3.UpdatePrice(new Money(1999.99m, "USD"));

            await _repository.AddAsync(product1);
            await _repository.AddAsync(product2);
            await _repository.AddAsync(product3);

            var filter = new ProductFilter
            {
                SearchTerm = "Apple",
                Category = "Electronics",
                MinPrice = 500,
                MaxPrice = 1500,
                Page = 1,
                PageSize = 10
            };

            var results = await _repository.GetFilteredAsync(filter);
            var totalCount = await _repository.GetTotalCountAsync(filter);

            results.Should().HaveCount(1, "Only one product should match all criteria: iPhone");
            var matchedProduct = results.First();
            matchedProduct.Name.Should().Contain("Apple");
            matchedProduct.Category.Should().Be(ProductCategory.Electronics);
            matchedProduct.Price.Amount.Should().BeLessThan(1500);
            matchedProduct.Price.Amount.Should().BeGreaterThan(500);
            totalCount.Should().Be(1);
        }

        [Test]
        public async Task GetFilteredAsyncWithInStockFilterShouldReturnOnlyInStockProducts()
        {
            var product1 = CreateTestProduct();
            var product2 = CreateTestProduct();
            var product3 = CreateTestProduct();

            await _repository.AddAsync(product1);
            await _repository.AddAsync(product2);
            await _repository.AddAsync(product3);

            var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(@"
            INSERT INTO product_stocks (id, product_id, quantity, reserved) 
            VALUES (@Id1, @ProductId1, 10, 0),
                   (@Id2, @ProductId2, 0, 0)",
                new
                {
                    Id1 = Guid.NewGuid(),
                    ProductId1 = product1.Id,
                    Id2 = Guid.NewGuid(),
                    ProductId2 = product2.Id
                }
            );

            var filter = new ProductFilter
            {
                InStockOnly = true,
                Page = 1,
                PageSize = 10
            };

            var results = await _repository.GetFilteredAsync(filter);
            var totalCount = await _repository.GetTotalCountAsync(filter);

            results.Should().HaveCount(1);
            results.First().Id.Should().Be(product1.Id);
            totalCount.Should().Be(1);
        }
    }
}
