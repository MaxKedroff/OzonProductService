using Application.DTOs;

namespace Application.Ports.Input
{
    public interface IStockService
    {
        Task<StockInfoDto> GetStockInfoAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<bool> TryReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
        Task<bool> CommitReservationAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
        Task<bool> CancelReservationAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
        Task AddStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
        Task RemoveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
    }
}
