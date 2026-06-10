using Application.DTOs;

namespace Application.Ports.Output
{
    public interface IProductCache
    {
        Task<ProductResponseDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task SetAsync(Guid id, ProductResponseDto product, TimeSpan ttl, CancellationToken cancellationToken = default);
        Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
        Task InvalidateSearchAsync(CancellationToken cancellationToken = default);
    }
}
