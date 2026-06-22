using Application.Events;
using Application.Ports.Input;
using Application.Ports.Output;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Handlers
{
    public class OrderCancelledEventHandler : IEventHandler<OrderCancelledEvent>
    {
        private readonly IStockService _stockService;
        private readonly IProductService _productService;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<OrderCancelledEventHandler> _logger;

        public OrderCancelledEventHandler(
            IStockService stockService,
            IProductService productService,
            IMessageBus messageBus,
            ILogger<OrderCancelledEventHandler> logger)
        {
            _stockService = stockService;
            _productService = productService;
            _messageBus = messageBus;
            _logger = logger;
        }

        public async Task HandleAsync(OrderCancelledEvent eventData, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Processing order cancellation: OrderId={OrderId}, Items={ItemCount}, Reason={Reason}",
                eventData.OrderId,
                eventData.Items.Count,
                eventData.Reason);

            var cancelledItems = new List<OrderCancelledItem>();
            var errors = new List<string>();

            foreach (var item in eventData.Items)
            {
                try
                {
                    var product = await _productService.GetByIdAsync(item.ProductId, cancellationToken);
                    if (product == null)
                    {
                        var error = $"Product {item.ProductId} not found, skipping cancellation";
                        _logger.LogWarning(error);
                        errors.Add(error);
                        continue;
                    }

                    var success = await _stockService.CancelReservationAsync(
                        item.ProductId,
                        item.Quantity,
                        cancellationToken);

                    if (success)
                    {
                        cancelledItems.Add(item);
                        _logger.LogInformation(
                            "Successfully cancelled reservation for product {ProductId}, quantity: {Quantity}",
                            item.ProductId,
                            item.Quantity);
                    }
                    else
                    {
                        var error = $"Failed to cancel reservation for product {item.ProductId}, quantity: {item.Quantity}";
                        _logger.LogWarning(error);
                        errors.Add(error);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Error cancelling reservation for product {item.ProductId}: {ex.Message}";
                    _logger.LogError(ex, "Error cancelling reservation for product {ProductId}, order {OrderId}",
                        item.ProductId, eventData.OrderId);
                    errors.Add(error);
                }
            }

            var responseEvent = new ReservationsCancelledEvent
            {
                OrderId = eventData.OrderId,
                Items = cancelledItems,
                CancelledAt = DateTime.UtcNow,
                Reason = eventData.Reason,
                IsComplete = cancelledItems.Count == eventData.Items.Count,
                Errors = errors.Any() ? errors : null
            };

            await _messageBus.PublishAsync(responseEvent, cancellationToken);

            _logger.LogInformation(
                "Completed processing order cancellation: OrderId={OrderId}, " +
                "Successfully cancelled {CancelledCount}/{TotalCount} items, " +
                "IsComplete={IsComplete}, Errors={ErrorsCount}",
                eventData.OrderId,
                cancelledItems.Count,
                eventData.Items.Count,
                responseEvent.IsComplete,
                errors.Count);
        }
    }
}
