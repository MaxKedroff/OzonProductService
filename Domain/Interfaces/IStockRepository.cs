using Domain.Models;

namespace Domain.Interfaces
{
    public interface IStockRepository
    {
        Task<ProductStock?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProductStock>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
        Task AddAsync(ProductStock stock, CancellationToken cancellationToken = default);
        Task UpdateAsync(ProductStock stock, CancellationToken cancellationToken = default);
        Task<bool> TryReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
        Task<bool> CommitReservationAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
        Task<bool> CancelReservationAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
    }
}
