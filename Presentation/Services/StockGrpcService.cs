using Application.Ports.Input;
using Grpc.Core;
using StockServiceGrpc.Grpc;

namespace Presentation.Services
{
    public class StockGrpcService : StockServiceGrpc.Grpc.StockServiceGrpc.StockServiceGrpcBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockGrpcService> _logger;

        public StockGrpcService(IStockService stockService, ILogger<StockGrpcService> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        public override async Task<StockInfoResponse> GetStockInfo(GetStockInfoRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC: Getting stock info for product: {ProductId}", request.ProductId);

            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format"));
            }

            var stock = await _stockService.GetStockInfoAsync(productId, context.CancellationToken);

            return new StockInfoResponse
            {
                ProductId = stock.ProductId.ToString(),
                Quantity = stock.Quantity,
                Reserved = stock.Reserved,
                Available = stock.Available,
                Warehouse = stock.Warehouse,
                LeadTimeDays = stock.LeadTimeDays,
                IsInStock = stock.IsInStock
            };
        }


        public override async Task<CommitReservationResponse> CommitReservation(CommitReservationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC: Committing reservation for {Quantity} units of product: {ProductId}",
                request.Quantity, request.ProductId);

            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format"));
            }

            if (request.Quantity <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Quantity must be positive"));
            }

            try
            {
                var success = await _stockService.CommitReservationAsync(productId, request.Quantity, context.CancellationToken);

                return new CommitReservationResponse
                {
                    Success = success,
                    Message = success ? "Reservation committed successfully" : "Failed to commit reservation"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing reservation for product {ProductId}", productId);
                throw new RpcException(new Status(StatusCode.Internal, "Error committing reservation: " + ex.Message));
            }
        }

        public override async Task<CancelReservationResponse> CancelReservation(CancelReservationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC: Cancelling reservation for {Quantity} units of product: {ProductId}",
                request.Quantity, request.ProductId);

            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format"));
            }

            if (request.Quantity <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Quantity must be positive"));
            }

            try
            {
                var success = await _stockService.CancelReservationAsync(productId, request.Quantity, context.CancellationToken);

                return new CancelReservationResponse
                {
                    Success = success,
                    Message = success ? "Reservation cancelled successfully" : "Failed to cancel reservation"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation for product {ProductId}", productId);
                throw new RpcException(new Status(StatusCode.Internal, "Error cancelling reservation: " + ex.Message));
            }
        }

        public override async Task<ReserveStockResponse> ReserveStock(StockServiceGrpc.Grpc.ReserveStockRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC: Reserving {Quantity} units of product: {ProductId}",
                request.Quantity, request.ProductId);

            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format"));
            }

            if (request.Quantity <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Quantity must be positive"));
            }

            try
            {
                var success = await _stockService.TryReserveStockAsync(productId, request.Quantity, context.CancellationToken);

                if (success)
                {
                    var updatedStock = await _stockService.GetStockInfoAsync(productId, context.CancellationToken);
                    return new ReserveStockResponse
                    {
                        Success = true,
                        Message = "Stock reserved successfully",
                        AvailableAfter = updatedStock?.Available ?? 0
                    };
                }
                else
                {
                    return new ReserveStockResponse
                    {
                        Success = false,
                        Message = "Insufficient stock available",
                        AvailableAfter = 0
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving stock for product {ProductId}", productId);
                throw new RpcException(new Status(StatusCode.Internal, "Error reserving stock: " + ex.Message));
            }
        }

        public override async Task<AddStockResponse> AddStock(StockServiceGrpc.Grpc.AddStockRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC: Adding {Quantity} units of stock for product: {ProductId}, warehouse: {Warehouse}",
                request.Quantity, request.ProductId, request.Warehouse);

            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format"));
            }

            if (request.Quantity <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Quantity must be positive"));
            }

            try
            {
                await _stockService.AddStockAsync(productId, request.Quantity, context.CancellationToken);

                var updatedStock = await _stockService.GetStockInfoAsync(productId, context.CancellationToken);

                return new AddStockResponse
                {
                    Success = true,
                    Message = $"Successfully added {request.Quantity} units of stock",
                    NewQuantity = updatedStock?.Quantity ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding stock for product {ProductId}", productId);
                throw new RpcException(new Status(StatusCode.Internal, "Error adding stock: " + ex.Message));
            }
        }
    }
}
