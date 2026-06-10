using Application.Events;
using Application.Ports.Input;
using Application.Ports.Output;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Handlers
{
    public class OrderPaidEventHandler : IEventHandler<OrderPaidEvent>
    {
        private readonly IStockService _stockService;
        private readonly ILogger<OrderPaidEventHandler> _logger;


        public OrderPaidEventHandler(IStockService stockService, ILogger<OrderPaidEventHandler> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        public async Task HandleAsync(OrderPaidEvent eventData, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing paid order {OrderId} with {ItemCount} items",
            eventData.OrderId, eventData.Items.Count);

            foreach (var item in eventData.Items)
            {
                try
                {
                    var commited = await _stockService.CommitReservationAsync(item.ProductId, item.Quantity, cancellationToken);

                    if (!commited)
                    {
                        _logger.LogWarning("Failed to commit reservation for product {ProductId}, quantity {Quantity}",
                        item.ProductId, item.Quantity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error committing reservation for product {ProductId}", item.ProductId);
                    throw;
                }
            }
        }
    }
}
