
using Application.DTOs;
using Application.Ports.Input;
using AutoMapper;
using Grpc.Core;
using ProductServiceGrpc.Grpc;

namespace Presentation.Services
{
    public class ProductGrpcService : ProductServiceGrpc.Grpc.ProductServiceGrpc.ProductServiceGrpcBase
    {
        private readonly IProductService _productService;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductGrpcService> _logger;

        public ProductGrpcService(
            IProductService productService,
            IMapper mapper,
            ILogger<ProductGrpcService> logger)
        {
            _productService = productService;
            _mapper = mapper;
            _logger = logger;
        }

        public override async Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC: Getting product with ID: {ProductId}", request.Id);
            if (!Guid.TryParse(request.Id, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format"));
            }

            var product = await _productService.GetByIdAsync(productId, context.CancellationToken);

            var response = new ProductResponse
            {
                Id = product.Id.ToString(),
                Name = product.Name,
                Description = product.Description ?? string.Empty,
                Price = (double)product.Price,
                Currency = product.Currency,
                Sku = product.Sku,
                Category = product.Category,
                AvailableStock = product.AvailableStock,
                IsInStock = product.IsInStock,
                LeadTimeDays = product.LeadTimeDays,
                CreatedAt = product.CreatedAt.ToString("o"),
                UpdatedAt = product.UpdatedAt?.ToString("o") ?? string.Empty
            };

            return response;
        }

        public override async Task<GetProductsResponse> GetProducts(GetProductsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC: Getting products with filter");
            var filter = new ProductFilterDto
            {
                Search = request.Search,
                Category = request.Category,
                MinPrice = request.MinPrice > 0 ? (decimal?)request.MinPrice : null,
                MaxPrice = request.MaxPrice > 0 ? (decimal?)request.MaxPrice : null,
                InStockOnly = request.InStockOnly,
                Page = request.Page > 0 ? request.Page : 1,
                PageSize = request.PageSize > 0 ? request.PageSize : 20,
                SortBy = string.IsNullOrEmpty(request.SortBy) ? "createdAt" : request.SortBy,
                SortDescending = request.SortDescending
            };

            var result = await _productService.GetFilteredAsync(filter, context.CancellationToken);

            var response = new GetProductsResponse
            {
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            foreach (var item in result.Items)
            {
                response.Items.Add(new ProductResponse
                {
                    Id = item.Id.ToString(),
                    Name = item.Name,
                    Description = item.Description ?? string.Empty,
                    Price = (double)item.Price,
                    Currency = item.Currency,
                    Sku = item.Sku,
                    Category = item.Category,
                    AvailableStock = item.AvailableStock,
                    IsInStock = item.IsInStock,
                    LeadTimeDays = item.LeadTimeDays,
                    CreatedAt = item.CreatedAt.ToString("o"),
                    UpdatedAt = item.UpdatedAt?.ToString("o") ?? string.Empty
                });
            }

            return response;
        }

        public override async Task<GetProductsBatchResponse> GetProductsBatch(GetProductsBatchRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC: Getting products batch with {Count} IDs", request.Ids.Count);

            var productIds = request.Ids
            .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToList();

            if (!productIds.Any())
            {
                return new GetProductsBatchResponse();
            }

            var products = await _productService.GetBatchAsync(productIds, context.CancellationToken);

            var response = new GetProductsBatchResponse();
            foreach (var item in products)
            {
                response.Items.Add(new ProductBatchItem
                {
                    Id = item.Id.ToString(),
                    Name = item.Name,
                    Price = (double)item.Price,
                    Currency = item.Currency,
                    AvailableStock = item.AvailableStock,
                    IsAvailable = item.IsAvailable,
                    LeadTimeDays = item.LeadTimeDays
                });
            }

            return response;
        }
    }
}
