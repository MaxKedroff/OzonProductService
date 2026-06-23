using Application.DTOs;
using Application.Exceptions;
using Application.Mappings;
using Application.Ports.Output;
using Application.Services;
using AutoMapper;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Models;
using Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Tests.Unit.Application.Services
{
    [TestFixture]
    public class ProductServiceTests
    {
        private Mock<IProductRepository> _productRepositoryMock;
        private Mock<IStockRepository> _stockRepositoryMock;
        private Mock<IProductCache> _cacheMock;
        private Mock<IMessageBus> _messageBusMock;
        private IMapper _mapper;
        private ProductService _productService;

        [SetUp]
        public void SetUp()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
            _stockRepositoryMock = new Mock<IStockRepository>();
            _cacheMock = new Mock<IProductCache>();
            _messageBusMock = new Mock<IMessageBus>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _productService = new ProductService(
            _productRepositoryMock.Object,
            _stockRepositoryMock.Object,
            _cacheMock.Object,
            _messageBusMock.Object,
            _mapper
            );
        }

        private Product CreateTestProduct()
        {
            return new Product(
                "Test Product",
                "Test Description",
                new Money(99.99m),
                new Sku("TEST-001"),
                ProductCategory.Electronics
            );
        }

        [Test]
        public async Task GetByIdAsyncWhenProductExistsShouldReturnProduct()
        {
            var product = CreateTestProduct();
            var productId = product.Id;
            var stock = new ProductStock(productId, 10);

            _cacheMock
                .Setup(x => x.GetAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductResponseDto?)null);

            _productRepositoryMock
                .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stock);

            var result = await _productService.GetByIdAsync(productId);

            result.Should().NotBeNull();
            result.Id.Should().Be(productId);
            result.Name.Should().Be("Test Product");
            result.Price.Should().Be(99.99m);
            result.AvailableStock.Should().Be(10);
            result.IsInStock.Should().BeTrue();

            _cacheMock.Verify(x => x.SetAsync(
                productId,
                It.IsAny<ProductResponseDto>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void GetByIdAsyncWhenProductNotFoundShouldThrowNotFoundException()
        {
            var productId = Guid.NewGuid();
            _cacheMock
                .Setup(x => x.GetAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductResponseDto?)null);

            _productRepositoryMock
                .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Func<Task> act = async () => await _productService.GetByIdAsync(productId);

            act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Product with id '{productId}' was not found");
        }

        [Test]
        public async Task GetByIdAsyncWhenProductInCacheShouldReturnFromCache()
        {
            var productId = Guid.NewGuid();

            var cachedProduct = new ProductResponseDto
            {
                Id = productId,
                Name = "Cached Product",
                Price = 99.99m
            };

            _cacheMock
            .Setup(x => x.GetAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedProduct);

            var result = await _productService.GetByIdAsync(productId);
            result.Should().Be(cachedProduct);
            _productRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        }

        [Test]
        public async Task CreateAsyncWithValidDataShouldCreateProduct()
        {
            var dto = new CreateProductDto
            {
                Name = "New Product",
                Description = "Description",
                Price = 49.99m,
                Currency = "USD",
                Sku = "NEW-001",
                Category = "Electronics",
                InitialStock = 5,
                Warehouse = "main",
                LeadTimeDays = 2
            };

            _productRepositoryMock
            .Setup(x => x.ExistsBySkuAsync(dto.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

            _productRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _stockRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _productService.CreateAsync(dto);

            result.Name.Should().Be("New Product");
            result.Price.Should().Be(49.99m);
            result.Sku.Should().Be("NEW-001");
            result.AvailableStock.Should().Be(5);
            result.IsInStock.Should().BeTrue();

            _productRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _stockRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _messageBusMock.Verify(
                x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void CreateAsyncWithDuplicateSkuShouldThrowBusinessRuleException()
        {
            var dto = new CreateProductDto
            {
                Name = "New Product",
                Description = "Description",
                Price = 49.99m,
                Currency = "USD",
                Sku = "EXISTING-001",
                Category = "Electronics",
                InitialStock = 0
            };

            _productRepositoryMock
            .Setup(x => x.ExistsBySkuAsync(dto.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

            Func<Task> act = async () => await _productService.CreateAsync(dto);

            act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage($"Product with SKU '{dto.Sku}' already exists");
        }

        [Test]
        public async Task UpdateAsyncWithValidDataShouldUpdateProduct()
        {
            var product = CreateTestProduct();
            var productId = product.Id;
            var stock = new ProductStock(productId, 10);
            var dto = new UpdateProductDto
            {
                Name = "Updated Product",
                Description = "Updated Description",
                Price = 149.99m
            };

            _productRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

            _productRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stock);

            _cacheMock
                .Setup(x => x.RemoveAsync(productId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _productService.UpdateAsync(productId, dto);

            result.Name.Should().Be("Updated Product");
            result.Description.Should().Be("Updated Description");
            result.Price.Should().Be(149.99m);

            _productRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _cacheMock.Verify(
                x => x.RemoveAsync(productId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void UpdateAsync_WhenProductNotFound_ShouldThrowNotFoundException()
        {
            var productId = Guid.NewGuid();
            var dto = new UpdateProductDto { Name = "Updated" };

            _productRepositoryMock
                .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Func<Task> act = async () => await _productService.UpdateAsync(productId, dto);

            act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Product with id '{productId}' was not found");
        }

        [Test]
        public async Task DeleteAsyncShouldSoftDeleteProduct()
        {
            var product = CreateTestProduct();
            var productId = product.Id;


            _productRepositoryMock
                .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            _productRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _cacheMock
                .Setup(x => x.RemoveAsync(productId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _productService.DeleteAsync(productId);

            product.IsDeleted.Should().BeTrue();
            _productRepositoryMock.Verify(
                x => x.UpdateAsync(product, It.IsAny<CancellationToken>()),
                Times.Once);

        }

        [Test]
        public async Task GetBatchAsync_ShouldReturnBatchOfProducts()
        {
            var product1 = CreateTestProduct();
            var product2 = CreateTestProduct();
            var productIds = new List<Guid> { product1.Id, product2.Id };
            var products = new List<Product> { product1, product2 };


            productIds = products.Select(el => el.Id).ToList();
            var stocks = new List<ProductStock>
            {
                new ProductStock(product1.Id, 10),
                new ProductStock(product2.Id, 10)
            };

            _productRepositoryMock
                .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stocks);

            var result = await _productService.GetBatchAsync(productIds);

            result.Should().HaveCount(2);
            foreach (var product in result)
            {
                product.AvailableStock.Should().Be(10);
                product.IsAvailable.Should().BeTrue();
            }
        }

        [Test]
        public async Task GetFilteredAsyncWithValidFilterShouldReturnPagedResult()
        {
            var filterDto = new ProductFilterDto
            {
                Search = "Test",
                Page = 1,
                PageSize = 10
            };

            var products = new List<Product>
            {
                CreateTestProduct(),
                CreateTestProduct()
            };

            var stocks = products.Select(p => new ProductStock(p.Id, 10)).ToList();

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            foreach (var product in products)
            {
                var stock = stocks.First(s => s.ProductId == product.Id);
                _stockRepositoryMock
                    .Setup(x => x.GetByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stock);
            }

            var result = await _productService.GetFilteredAsync(filterDto);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalPages.Should().Be(1);
            result.HasPrevious.Should().BeFalse();
            result.HasNext.Should().BeFalse();

            foreach (var item in result.Items)
            {
                item.AvailableStock.Should().Be(10);
                item.IsInStock.Should().BeTrue();
                item.LeadTimeDays.Should().Be(3);
            }
        }

        [Test]
        public async Task GetFilteredAsyncWithNoResultsShouldReturnEmptyPagedResult()
        {
            var filterDto = new ProductFilterDto
            {
                Search = "NonExistent",
                Page = 1,
                PageSize = 10
            };

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product>());

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            var result = await _productService.GetFilteredAsync(filterDto);

            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalPages.Should().Be(0);
            result.HasPrevious.Should().BeFalse();
            result.HasNext.Should().BeFalse();
        }

        [Test]
        public async Task GetFilteredAsyncWithProductsWithoutStockShouldSetDefaultStockValues()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 1,
                PageSize = 10
            };

            var products = new List<Product>
            {
                CreateTestProduct()
            };

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(products[0].Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductStock?)null);

            var result = await _productService.GetFilteredAsync(filterDto);

            result.Items.Should().HaveCount(1);
            var item = result.Items.First();
            item.AvailableStock.Should().Be(0);
            item.IsInStock.Should().BeFalse();
            item.LeadTimeDays.Should().Be(0);
        }

        [Test]
        public async Task GetFilteredAsync_WithPagination_ShouldReturnCorrectPage()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 2,
                PageSize = 5
            };

            var products = new List<Product>
            {
                CreateTestProduct(),
                CreateTestProduct(),
                CreateTestProduct(),
                CreateTestProduct(),
                CreateTestProduct()
            };

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(12);

            foreach (var product in products)
            {
                _stockRepositoryMock
                    .Setup(x => x.GetByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ProductStock(product.Id, 5));
            }

            var result = await _productService.GetFilteredAsync(filterDto);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(12);
            result.Page.Should().Be(2);
            result.PageSize.Should().Be(5);
            result.TotalPages.Should().Be(3);
            result.HasPrevious.Should().BeTrue();
            result.HasNext.Should().BeTrue();
        }

        [Test]
        public async Task GetFilteredAsyncWithSortingShouldPassCorrectSortParameters()
        {
            var filterDto = new ProductFilterDto
            {
                SortBy = "price",
                SortDescending = true,
                Page = 1,
                PageSize = 10
            };

            var products = new List<Product>();
            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            await _productService.GetFilteredAsync(filterDto);

            _productRepositoryMock.Verify(
                x => x.GetFilteredAsync(
                    It.Is<ProductFilter>(f => f.SortBy == "price" && f.SortDescending == true),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetFilteredAsyncWithMultipleFiltersShouldApplyAllFilters()
        {
            var filterDto = new ProductFilterDto
            {
                Search = "iPhone",
                Category = "Electronics",
                MinPrice = 500,
                MaxPrice = 1500,
                InStockOnly = true,
                Page = 1,
                PageSize = 20
            };

            var products = new List<Product>
            {
                CreateTestProduct()
            };

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductStock(Guid.NewGuid(), 10));

            var result = await _productService.GetFilteredAsync(filterDto);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);

            _productRepositoryMock.Verify(
                x => x.GetFilteredAsync(
                    It.Is<ProductFilter>(f =>
                        f.SearchTerm == "iPhone" &&
                        f.Category == "Electronics" &&
                        f.MinPrice == 500 &&
                        f.MaxPrice == 1500 &&
                        f.InStockOnly == true),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void GetFilteredAsync_WithInvalidFilter_ShouldThrowValidationException()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 0,
                PageSize = 10
            };

            Func<Task> act = async () => await _productService.GetFilteredAsync(filterDto);

            act.Should().ThrowAsync<FluentValidation.ValidationException>();

            _productRepositoryMock.Verify(
                x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void GetFilteredAsyncWithTooLargePageSizeShouldThrowValidationException()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 1,
                PageSize = 101
            };

            Func<Task> act = async () => await _productService.GetFilteredAsync(filterDto);

            act.Should().ThrowAsync<FluentValidation.ValidationException>();

            _productRepositoryMock.Verify(
                x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void GetFilteredAsync_WithMinPriceGreaterThanMaxPrice_ShouldThrowValidationException()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 1,
                PageSize = 10,
                MinPrice = 1000,
                MaxPrice = 500
            };

            Func<Task> act = async () => await _productService.GetFilteredAsync(filterDto);

            act.Should().ThrowAsync<FluentValidation.ValidationException>()
                .WithMessage("Min price cannot be greater than max price");
        }

        [Test]
        public async Task GetFilteredAsync_WithLastPage_ShouldNotHaveNextPage()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 3,
                PageSize = 5
            };

            var products = new List<Product>
            {
                CreateTestProduct(),
                CreateTestProduct(),
                CreateTestProduct()
            };

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(13); 

            var result = await _productService.GetFilteredAsync(filterDto);

            result.TotalPages.Should().Be(3);
            result.HasPrevious.Should().BeTrue();
            result.HasNext.Should().BeFalse();
        }

        [Test]
        public async Task GetFilteredAsyncWithFirstPageShouldNotHavePreviousPage()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 1,
                PageSize = 5
            };

            var products = new List<Product>
            {
                CreateTestProduct(),
                CreateTestProduct()
            };

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            var result = await _productService.GetFilteredAsync(filterDto);

            result.HasPrevious.Should().BeFalse();
            result.HasNext.Should().BeFalse();
            result.TotalPages.Should().Be(1);
        }

        [Test]
        public async Task GetFilteredAsyncWithStockInformationShouldPopulateStockFields()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 1,
                PageSize = 10
            };

            var product = CreateTestProduct();
            var stock = new ProductStock(product.Id, 25, "main", 5);
            stock.TryReserve(5); 

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stock);

            var result = await _productService.GetFilteredAsync(filterDto);

            var item = result.Items.First();
            item.AvailableStock.Should().Be(20);
            item.IsInStock.Should().BeTrue();
            item.LeadTimeDays.Should().Be(5);
        }

        [Test]
        public async Task GetFilteredAsyncShouldMapProductCorrectly()
        {
            var filterDto = new ProductFilterDto
            {
                Page = 1,
                PageSize = 10
            };

            var product = CreateTestProduct();
            var stock = new ProductStock(product.Id, 15);

            _productRepositoryMock
                .Setup(x => x.GetFilteredAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            _productRepositoryMock
                .Setup(x => x.GetTotalCountAsync(It.IsAny<ProductFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stock);

            var result = await _productService.GetFilteredAsync(filterDto);

            var item = result.Items.First();
            item.Id.Should().Be(product.Id);
            item.Name.Should().Be(product.Name);
            item.Description.Should().Be(product.Description);
            item.Price.Should().Be(product.Price.Amount);
            item.Currency.Should().Be(product.Price.Currency);
            item.Sku.Should().Be(product.Sku.Value);
            item.Category.Should().Be(product.Category.ToString());
            item.CreatedAt.Should().Be(product.CreatedAt);
        }
    }
}
