using Application.DTOs;
using Application.Ports.Input;
using Application.Ports.Output;
using Application.Exceptions;
using Domain.Interfaces;
using Domain.Models;
using Domain.ValueObjects;
using AutoMapper;
using Domain.Enums;
using Application.Events;
using Application.Validators;
using FluentValidation;


namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IProductCache _cache;
        private readonly IMessageBus _messageBus;
        private readonly IMapper _mapper;
        public ProductService(
        IProductRepository productRepository,
        IStockRepository stockRepository,
        IProductCache cache,
        IMessageBus messageBus,
        IMapper mapper)
        {
            _productRepository = productRepository;
            _stockRepository = stockRepository;
            _cache = cache;
            _messageBus = messageBus;
            _mapper = mapper;
        }

        public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
        {
            var validator = new CreateProductValidator();
            await validator.ValidateAndThrowAsync(dto, cancellationToken);
            if (await _productRepository.ExistsBySkuAsync(dto.Sku, cancellationToken))
                throw new BusinessRuleException($"Product with SKU '{dto.Sku}' already exists");

            var category = Enum.Parse<ProductCategory>(dto.Category, true);
            var price = new Money(dto.Price, dto.Currency);
            var sku = new Sku(dto.Sku);
            var product = new Product(dto.Name, dto.Description, price, sku, category);
            await _productRepository.AddAsync(product, cancellationToken);
            if (dto.InitialStock > 0)
            {
                var stock = new ProductStock(product.Id, dto.InitialStock, dto.Warehouse, dto.LeadTimeDays);
                await _stockRepository.AddAsync(stock, cancellationToken);
            }

            await _messageBus.PublishAsync(new ProductCreatedEvent
            {
                ProductId = product.Id,
                Name = product.Name,
                Sku = product.Sku.Value,
                Price = product.Price.Amount,
                Category = product.Category.ToString()
            }, cancellationToken);

            var response = _mapper.Map<ProductResponseDto>(product);
            response.AvailableStock = dto.InitialStock;
            response.IsInStock = dto.InitialStock > 0;
            response.LeadTimeDays = dto.LeadTimeDays;

            return response;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
                throw new NotFoundException(nameof(Product), id);

            product.SoftDelete();
            await _productRepository.UpdateAsync(product, cancellationToken);
            await _cache.RemoveAsync(id, cancellationToken);
        }

        public async Task<IEnumerable<ProductBatchDto>> GetBatchAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
        {
            var ids = productIds.Distinct().ToList();
            var products = await _productRepository.GetByIdsAsync(ids, cancellationToken);
            var stocks = await _stockRepository.GetByProductIdsAsync(ids, cancellationToken);
            var stockDict = stocks.ToDictionary(s => s.ProductId, s => s);
            return products.Select(product =>
            {
                var stock = stockDict.GetValueOrDefault(product.Id);
                return new ProductBatchDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price.Amount,
                    Currency = product.Price.Currency,
                    AvailableStock = stock?.AvailableQuantity ?? 0,
                    IsAvailable = stock?.IsInStock ?? false,
                    LeadTimeDays = stock?.LeadTimeDays ?? 0
                };
            });
        }

        public async Task<ProductResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync(id, cancellationToken);
            if (cached != null)
                return cached;

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
                throw new NotFoundException(nameof(Product), id);

            var dto = _mapper.Map<ProductResponseDto>(product);

            var stock = await _stockRepository.GetByProductIdAsync(id, cancellationToken);
            if (stock != null)
            {
                dto.AvailableStock = stock.AvailableQuantity;
                dto.IsInStock = stock.IsInStock;
                dto.LeadTimeDays = stock.LeadTimeDays;
            }
            await _cache.SetAsync(id, dto, TimeSpan.FromMinutes(10), cancellationToken);
            return dto;
        }

        public IEnumerable<string> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return Enum.GetNames<ProductCategory>().Select(x => x.ToString());
        }

        public async Task<PagedResult<ProductResponseDto>> GetFilteredAsync(ProductFilterDto filterDto, CancellationToken cancellationToken = default)
        {
            var validator = new ProductFilterValidator();
            await validator.ValidateAndThrowAsync(filterDto, cancellationToken);

            var filter = _mapper.Map<ProductFilter>(filterDto);

            var products = await _productRepository.GetFilteredAsync(filter, cancellationToken);
            var totalCount = await _productRepository.GetTotalCountAsync(filter, cancellationToken);
            var items = new List<ProductResponseDto>();
            foreach (var product in products)
            {
                var dto = _mapper.Map<ProductResponseDto>(product);
                var stock = await _stockRepository.GetByProductIdAsync(product.Id, cancellationToken);

                if (stock != null)
                {
                    dto.AvailableStock = stock.AvailableQuantity;
                    dto.IsInStock = stock.IsInStock;
                    dto.LeadTimeDays = stock.LeadTimeDays;
                }
                items.Add(dto);
            }

            return new PagedResult<ProductResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ProductResponseDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
        {
            var validator = new UpdateProductValidator();
            await validator.ValidateAndThrowAsync(dto, cancellationToken);

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
                throw new NotFoundException(nameof(Product), id);

            product.UpdateDetails(
               dto.Name ?? product.Name,
               dto.Description ?? product.Description,
               product.Category);

            if (dto.Price.HasValue)
            {
                product.UpdatePrice(new Money(dto.Price.Value, product.Price.Currency));
            }

            await _productRepository.UpdateAsync(product, cancellationToken);

            await _cache.RemoveAsync(id, cancellationToken);

            var stock = await _stockRepository.GetByProductIdAsync(id, cancellationToken);
            var response = _mapper.Map<ProductResponseDto>(product);

            if (stock != null)
            {
                response.AvailableStock = stock.AvailableQuantity;
                response.IsInStock = stock.IsInStock;
                response.LeadTimeDays = stock.LeadTimeDays;
            }
            return response;
        }
    }
}
