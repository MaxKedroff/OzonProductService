using Dapper;
using Domain.Interfaces;
using Domain.Models;
using Domain.ValueObjects;

namespace Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProductRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO products (id, name, description, price, currency, sku, category, is_deleted, created_at)
                VALUES (@Id, @Name, @Description, @Price, @Currency, @Sku, @Category, @IsDeleted, @CreatedAt)";

            await connection.ExecuteAsync(sql, new
            {
                product.Id,
                product.Name,
                product.Description,
                Price = product.Price.Amount,
                Currency = product.Price.Currency,
                Sku = product.Sku.Value,
                Category = product.Category.ToString(),
                product.IsDeleted,
                product.CreatedAt
            });
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "UPDATE products SET is_deleted = true, updated_at = NOW() WHERE id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT EXISTS(SELECT 1 FROM products WHERE id = @Id AND is_deleted = false)";
            return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
        }

        public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT EXISTS(SELECT 1 FROM products WHERE sku = @Sku AND is_deleted = false)";
            return await connection.ExecuteScalarAsync<bool>(sql, new { Sku = sku });
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    id, name, description, price, currency, sku, category, 
                    is_deleted, created_at, updated_at
                FROM products 
                WHERE id = @Id AND is_deleted = false";

            var result = await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
            return result;
        }

        public async Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    id, name, description, price, currency, sku, category, 
                    is_deleted, created_at, updated_at
                FROM products 
                WHERE id = ANY(@Ids) AND is_deleted = false";

            var result = await connection.QueryAsync<Product>(sql, new { Ids = ids.ToList() });
            return result;
        }

        public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    id, name, description, price, currency, sku, category, 
                    is_deleted, created_at, updated_at
                FROM products 
                WHERE sku = @Sku AND is_deleted = false";
            var result = await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Sku = sku });
            return result;
        }

        public async Task<IEnumerable<Product>> GetFilteredAsync(ProductFilter filter, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT 
                    p.id, p.name, p.description, p.price, p.currency, p.sku, p.category, 
                    p.is_deleted, p.created_at, p.updated_at
                FROM products p
                WHERE p.is_deleted = false";

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (filter.HasCategory)
            {
                conditions.Add("p.category = @Category");
                parameters.Add("Category", filter.Category);
            }

            if (filter.HasSearch)
            {
                conditions.Add("p.name ILIKE @SearchPattern");
                parameters.Add("SearchPattern", $"%{filter.SearchTerm}%");
            }

            if (filter.HasPriceRange)
            {
                if (filter.MinPrice.HasValue)
                {
                    conditions.Add("p.price >= @MinPrice");
                    parameters.Add("MinPrice", filter.MinPrice.Value);
                }
                if (filter.MaxPrice.HasValue)
                {
                    conditions.Add("p.price <= @MaxPrice");
                    parameters.Add("MaxPrice", filter.MaxPrice.Value);
                }
            }

            if (filter.InStockOnly.HasValue && filter.InStockOnly.Value)
            {
                conditions.Add(@"EXISTS (
                    SELECT 1 FROM product_stocks s 
                    WHERE s.product_id = p.id AND (s.quantity - s.reserved) > 0
                )");
            }

            if (conditions.Any())
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            var sortColumn = filter.SortBy?.ToLower() switch
            {
                "name" => "p.name",
                "price" => "p.price",
                "createdat" => "p.created_at",
                "updatedat" => "p.updated_at",
                _ => "p.created_at"
            };

            sql += $" ORDER BY {sortColumn} {(filter.SortDescending ? "DESC" : "ASC")}";
            sql += " LIMIT @PageSize OFFSET @Offset";

            parameters.Add("PageSize", filter.PageSize);
            parameters.Add("Offset", filter.Offset);

            var result = await connection.QueryAsync<Product>(sql, parameters);
            return result;
        }

        public async Task<int> GetTotalCountAsync(ProductFilter filter, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM products p
                WHERE p.is_deleted = false";

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (filter.HasCategory)
            {
                conditions.Add("p.category = @Category");
                parameters.Add("Category", filter.Category);
            }

            if (filter.HasSearch)
            {
                conditions.Add("p.name ILIKE @SearchPattern");
                parameters.Add("SearchPattern", $"%{filter.SearchTerm}%");
            }

            if (filter.HasPriceRange)
            {
                if (filter.MinPrice.HasValue)
                {
                    conditions.Add("p.price >= @MinPrice");
                    parameters.Add("MinPrice", filter.MinPrice.Value);
                }
                if (filter.MaxPrice.HasValue)
                {
                    conditions.Add("p.price <= @MaxPrice");
                    parameters.Add("MaxPrice", filter.MaxPrice.Value);
                }
            }

            if (filter.InStockOnly.HasValue && filter.InStockOnly.Value)
            {
                conditions.Add(@"EXISTS (
                    SELECT 1 FROM product_stocks s 
                    WHERE s.product_id = p.id AND (s.quantity - s.reserved) > 0
                )");
            }

            if (conditions.Any())
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            var result = await connection.ExecuteScalarAsync<int>(sql, parameters);
            return result;
        }

        public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
            UPDATE products 
            SET name = @Name, 
                description = @Description, 
                price = @Price,
                currency = @Currency,
                sku = @Sku,
                category = @Category,
                is_deleted = @IsDeleted,
                updated_at = @UpdatedAt
            WHERE id = @Id";

            await connection.ExecuteAsync(sql, new
            {
                product.Id,
                product.Name,
                product.Description,
                Price = product.Price.Amount,
                Currency = product.Price.Currency,
                Sku = product.Sku.Value,
                Category = product.Category.ToString(),
                product.IsDeleted,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
