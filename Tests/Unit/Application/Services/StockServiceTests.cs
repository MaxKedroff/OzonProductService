using Application.Exceptions;
using Application.Ports.Output;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Moq;
using Domain.Exceptions;

namespace Tests.Unit.Application.Services
{
    [TestFixture]
    public class StockServiceTests
    {
        private Mock<IStockRepository> _stockRepositoryMock;
        private Mock<IProductRepository> _productRepositoryMock;
        private Mock<IMessageBus> _messageBusMock;
        private StockService _stockService;

        [SetUp]
        public void SetUp()
        {
            _stockRepositoryMock = new Mock<IStockRepository>();
            _messageBusMock = new Mock<IMessageBus>();

            _stockService = new StockService(
                _stockRepositoryMock.Object,
                _messageBusMock.Object
            );
        }

        [Test]
        public async Task GetStockInfoAsyncWhenStockExistsShouldReturnStockInfo()
        {
            var productId = Guid.NewGuid();
            var stock = new ProductStock(productId, 10, "main", 3);
            stock.TryReserve(3);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stock);

            var result = await _stockService.GetStockInfoAsync(productId);

            result.ProductId.Should().Be(productId);
            result.Quantity.Should().Be(10);
            result.Reserved.Should().Be(3);
            result.Available.Should().Be(7);
            result.Warehouse.Should().Be("main");
            result.LeadTimeDays.Should().Be(3);
            result.IsInStock.Should().BeTrue();
        }

        [Test]
        public async Task GetStockInfoAsyncWhenStockNotExistsShouldReturnDefault()
        {
            var productId = Guid.NewGuid();

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductStock?)null);

            var result = await _stockService.GetStockInfoAsync(productId);

            result.ProductId.Should().Be(productId);
            result.IsInStock.Should().BeFalse();
            result.Quantity.Should().Be(0);
            result.Reserved.Should().Be(0);
            result.Available.Should().Be(0);
            result.Warehouse.Should().BeEmpty();
            result.LeadTimeDays.Should().Be(0);
        }

        [Test]
        public async Task TryReserveStockAsyncWhenSuccessfulShouldReturnTrueAndPublishEvent()
        {
            var productId = Guid.NewGuid();
            var quantity = 5;

            _stockRepositoryMock
                .Setup(x => x.TryReserveStockAsync(productId, quantity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _stockService.TryReserveStockAsync(productId, quantity);

            result.Should().BeTrue();
            _messageBusMock.Verify(
                x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task TryReserveStockAsyncWhenFailedShouldReturnFalseAndNotPublishEvent()
        {
            var productId = Guid.NewGuid();
            var quantity = 5;

            _stockRepositoryMock
                .Setup(x => x.TryReserveStockAsync(productId, quantity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _stockService.TryReserveStockAsync(productId, quantity);

            result.Should().BeFalse();
            _messageBusMock.Verify(
                x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task CommitReservationAsyncWhenSuccessfulShouldReturnTrueAndPublishEvent()
        {
            var productId = Guid.NewGuid();
            var quantity = 5;

            _stockRepositoryMock
                .Setup(x => x.CommitReservationAsync(productId, quantity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _stockService.CommitReservationAsync(productId, quantity);

            result.Should().BeTrue();
            _messageBusMock.Verify(
                x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task CommitReservationAsync_WhenFailed_ShouldReturnFalseAndNotPublishEvent()
        {
            var productId = Guid.NewGuid();
            var quantity = 5;

            _stockRepositoryMock
                .Setup(x => x.CommitReservationAsync(productId, quantity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _stockService.CommitReservationAsync(productId, quantity);

            result.Should().BeFalse();
            _messageBusMock.Verify(
                x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task CancelReservationAsync_ShouldReturnRepositoryResult()
        {
            var productId = Guid.NewGuid();
            var quantity = 5;

            _stockRepositoryMock
                .Setup(x => x.CancelReservationAsync(productId, quantity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _stockService.CancelReservationAsync(productId, quantity);

            result.Should().BeTrue();
        }

        [Test]
        public async Task AddStockAsyncWhenStockExistsShouldUpdateExistingStock()
        {
            var productId = Guid.NewGuid();
            var quantity = 10;
            var existingStock = new ProductStock(productId, 5);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingStock);

            _stockRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _stockService.AddStockAsync(productId, quantity);

            existingStock.Quantity.Should().Be(15); 
            _stockRepositoryMock.Verify(
                x => x.UpdateAsync(existingStock, It.IsAny<CancellationToken>()),
                Times.Once);
            _stockRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task AddStockAsyncWhenStockNotExistsShouldCreateNewStock()
        {
            var productId = Guid.NewGuid();
            var quantity = 10;

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductStock?)null);

            _stockRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _stockService.AddStockAsync(productId, quantity);
            _stockRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()),
            Times.Once);
            _stockRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void RemoveStockAsyncWhenStockExistsShouldRemoveStock()
        {
            var productId = Guid.NewGuid();
            var quantity = 3;
            var existingStock = new ProductStock(productId, 10);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingStock);

            _stockRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Func<Task> act = async () => await _stockService.RemoveStockAsync(productId, quantity);

            act.Should().NotThrowAsync();
            existingStock.Quantity.Should().Be(7);
            _stockRepositoryMock.Verify(
                x => x.UpdateAsync(existingStock, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void RemoveStockAsyncWhenStockNotExistsShouldThrowNotFoundException()
        {
            var productId = Guid.NewGuid();
            var quantity = 3;

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductStock?)null);

            Func<Task> act = async () => await _stockService.RemoveStockAsync(productId, quantity);

            act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"ProductStock with id '{productId}' was not found");
        }

        [Test]
        public void RemoveStockAsyncWhenInsufficientStockShouldThrowDomainException()
        {
            var productId = Guid.NewGuid();
            var quantity = 15;
            var existingStock = new ProductStock(productId, 10);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingStock);

            Func<Task> act = async () => await _stockService.RemoveStockAsync(productId, quantity);

            act.Should().ThrowAsync<DomainException>()
                .WithMessage($"Insufficient available stock. Available: 10");
            _stockRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task MultipleOperationsShouldWorkCorrectly()
        {
            var productId = Guid.NewGuid();
            var stock = new ProductStock(productId, 20);

            _stockRepositoryMock
                .Setup(x => x.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stock);

            _stockRepositoryMock
                .Setup(x => x.TryReserveStockAsync(productId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _stockRepositoryMock
                .Setup(x => x.CommitReservationAsync(productId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _stockRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _stockService.AddStockAsync(productId, 10);
            var reserveResult = await _stockService.TryReserveStockAsync(productId, 5);
            var commitResult = await _stockService.CommitReservationAsync(productId, 5);

            reserveResult.Should().BeTrue();
            commitResult.Should().BeTrue();

            _stockRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<ProductStock>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
    }
}
