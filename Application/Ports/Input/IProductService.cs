using Application.DTOs;

namespace Application.Ports.Input
{
    public interface IProductService
    {
        Task<ProductResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PagedResult<ProductResponseDto>> GetFilteredAsync(ProductFilterDto filter, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProductBatchDto>> GetBatchAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
        Task<ProductResponseDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
