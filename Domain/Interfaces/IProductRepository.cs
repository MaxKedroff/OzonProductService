using Domain.Models;
using Domain.ValueObjects;

namespace Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetFilteredAsync(ProductFilter filter, CancellationToken cancellationToken = default);
        Task<int> GetTotalCountAsync(ProductFilter filter, CancellationToken cancellationToken = default);
        Task AddAsync(Product product, CancellationToken cancellationToken = default);
        Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);
    }
}
