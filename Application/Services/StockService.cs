using Application.DTOs;
using Application.Events;
using Application.Exceptions;
using Application.Ports.Input;
using Application.Ports.Output;
using Domain.Interfaces;
using Domain.Models;

namespace Application.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IMessageBus _messageBus;

        public StockService(
            IStockRepository stockRepository,
            IMessageBus messageBus)
        {
            _stockRepository = stockRepository;
            _messageBus = messageBus;
        }

        public async Task AddStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            var stock = await _stockRepository.GetByProductIdAsync(productId, cancellationToken);
            if (stock == null)
            {
                stock = new ProductStock(productId, quantity);
                await _stockRepository.AddAsync(stock, cancellationToken);
            }
            else
            {
                stock.AddStock(quantity);
                await _stockRepository.UpdateAsync(stock, cancellationToken);
            }
        }

        public async Task<bool> CancelReservationAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            return await _stockRepository.CancelReservationAsync(productId, quantity, cancellationToken);
        }

        public async Task<bool> CommitReservationAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            var result = await _stockRepository.CommitReservationAsync(productId, quantity, cancellationToken);

            if (result)
            {
                await _messageBus.PublishAsync(new StockCommittedEvent
                {
                    ProductId = productId,
                    Quantity = quantity
                }, cancellationToken);
            }

            return result;
        }

        public async Task<StockInfoDto> GetStockInfoAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var stock = await _stockRepository.GetByProductIdAsync(productId, cancellationToken);
            if (stock == null)
                return new StockInfoDto { ProductId = productId, IsInStock = false };

            return new StockInfoDto
            {
                ProductId = stock.ProductId,
                Quantity = stock.Quantity,
                Reserved = stock.Reserved,
                Available = stock.AvailableQuantity,
                Warehouse = stock.Warehouse,
                LeadTimeDays = stock.LeadTimeDays,
                IsInStock = stock.IsInStock
            };
        }

        public async Task RemoveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            var stock = await _stockRepository.GetByProductIdAsync(productId, cancellationToken);
            if (stock == null)
                throw new NotFoundException(nameof(ProductStock), productId);

            stock.RemoveStock(quantity);
            await _stockRepository.UpdateAsync(stock, cancellationToken);
        }

        public async Task<bool> TryReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            var result = await _stockRepository.TryReserveStockAsync(productId, quantity, cancellationToken);

            if (result)
            {
                await _messageBus.PublishAsync(new StockReservedEvent
                {
                    ProductId = productId,
                    Quantity = quantity
                }, cancellationToken);
            }

            return result;
        }
    }
}
