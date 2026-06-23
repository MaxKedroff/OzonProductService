using Dapper;
using Domain.Interfaces;
using Domain.Models;

namespace Infrastructure.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public StockRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task AddAsync(ProductStock stock, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO product_stocks (id, product_id, quantity, reserved, warehouse, lead_time_days, created_at)
                VALUES (@Id, @ProductId, @Quantity, @Reserved, @Warehouse, @LeadTimeDays, @CreatedAt)";

            await connection.ExecuteAsync(sql, new
            {
                stock.Id,
                stock.ProductId,
                stock.Quantity,
                stock.Reserved,
                stock.Warehouse,
                LeadTimeDays = stock.LeadTimeDays,
                stock.CreatedAt
            });
        }

        public async Task<bool> CancelReservationAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE product_stocks
                SET reserved = reserved - @Quantity,
                    updated_at = NOW()
                WHERE product_id = @ProductId 
                    AND reserved >= @Quantity
                RETURNING id";

            var result = await connection.ExecuteScalarAsync<Guid?>(sql, new { ProductId = productId, Quantity = quantity });
            return result.HasValue;
        }

        public async Task<bool> CommitReservationAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE product_stocks
                SET quantity = quantity - @Quantity,
                    reserved = reserved - @Quantity,
                    updated_at = NOW()
                WHERE product_id = @ProductId 
                    AND reserved >= @Quantity
                RETURNING id";

            var result = await connection.ExecuteScalarAsync<Guid?>(sql, new { ProductId = productId, Quantity = quantity });
            return result.HasValue;
        }

        public async Task<ProductStock?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    id, 
                    product_id as ProductId, 
                    quantity, 
                    reserved, 
                    warehouse, 
                    lead_time_days as LeadTimeDays, 
                    created_at as CreatedAt, 
                    updated_at as UpdatedAt
                FROM product_stocks 
                WHERE product_id = @ProductId";

            var result = await connection.QueryFirstOrDefaultAsync<ProductStock>(sql, new { ProductId = productId });
            return result;
        }

        public async Task<IEnumerable<ProductStock>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    id, 
                    product_id as ProductId, 
                    quantity, 
                    reserved, 
                    warehouse, 
                    lead_time_days as LeadTimeDays, 
                    created_at as CreatedAt, 
                    updated_at as UpdatedAt
                FROM product_stocks 
                WHERE product_id = ANY(@ProductIds)";

            var result = await connection.QueryAsync<ProductStock>(sql, new { ProductIds = productIds.ToList() });
            return result;
        }

        public async Task<bool> TryReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE product_stocks
                SET reserved = reserved + @Quantity,
                    updated_at = NOW()
                WHERE product_id = @ProductId 
                    AND (quantity - reserved) >= @Quantity
                RETURNING id";

            var result = await connection.ExecuteScalarAsync<Guid?>(sql, new { ProductId = productId, Quantity = quantity });
            return result.HasValue;
        }

        public async Task UpdateAsync(ProductStock stock, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE product_stocks 
                SET quantity = @Quantity, 
                    reserved = @Reserved,
                    warehouse = @Warehouse,
                    lead_time_days = @LeadTimeDays,
                    updated_at = NOW()
                WHERE product_id = @ProductId";

            await connection.ExecuteAsync(sql, new
            {
                stock.Quantity,
                stock.Reserved,
                stock.Warehouse,
                LeadTimeDays = stock.LeadTimeDays,
                stock.ProductId
            });
        }
    }
}
