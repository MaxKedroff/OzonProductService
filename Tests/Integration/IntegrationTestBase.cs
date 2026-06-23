using Dapper;
using Infrastructure;
using Npgsql;
using StackExchange.Redis;
using System.Data;

namespace Tests.Integration
{
    public abstract class IntegrationTestBase
    {
        protected IDbConnectionFactory _connectionFactory = null!;
        protected IDbConnection _connection = null!;
        protected string _connectionString = null!;
        protected string _databaseName = "productservicedb_test";

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var host = "161.104.19.132";
            var port = 5432;
            var username = "postgres";
            var password = "postgres";


            var masterConnectionString = $"Host={host};Port={port};Database=postgres;Username={username};Password={password}";
            using var masterConnection = new NpgsqlConnection(masterConnectionString);
            await masterConnection.OpenAsync();

            await masterConnection.ExecuteAsync($"DROP DATABASE IF EXISTS {_databaseName}");
            await masterConnection.ExecuteAsync($"CREATE DATABASE {_databaseName}");




            await masterConnection.CloseAsync();

            _connectionString = $"Host={host};Port={port};Database={_databaseName};Username={username};Password={password}";
            _connectionFactory = new NpgsqlConnectionFactory(_connectionString);
            _connection = _connectionFactory.CreateConnection();

            await CreateTablesAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            //var masterConnectionString = $"Host=161.104.19.132;Port=5432;Database=postgres;Username=postgres;Password=postgres";
            //using var masterConnection = new NpgsqlConnection(masterConnectionString);
            //await masterConnection.ExecuteAsync($"DROP DATABASE IF EXISTS {_databaseName}");
            _connection?.Dispose();
        }

        [SetUp]
        public async Task SetUp()
        {
            await _connection.ExecuteAsync("TRUNCATE TABLE product_stocks CASCADE");
            await _connection.ExecuteAsync("TRUNCATE TABLE products CASCADE");
        }

        private async Task CreateTablesAsync()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS products (
                    id UUID PRIMARY KEY,
                    name VARCHAR(200) NOT NULL,
                    description TEXT,
                    price DECIMAL(12,2) NOT NULL,
                    currency VARCHAR(3) NOT NULL,
                    sku VARCHAR(50) NOT NULL UNIQUE,
                    category VARCHAR(100) NOT NULL,
                    is_deleted BOOLEAN DEFAULT FALSE,
                    created_at TIMESTAMP DEFAULT NOW(),
                    updated_at TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS product_stocks (
                    id UUID PRIMARY KEY,
                    product_id UUID NOT NULL,
                    quantity INT NOT NULL DEFAULT 0,
                    reserved INT NOT NULL DEFAULT 0,
                    warehouse VARCHAR(100) NOT NULL DEFAULT 'main',
                    lead_time_days INT NOT NULL DEFAULT 3,
                    created_at TIMESTAMP DEFAULT NOW(),
                    updated_at TIMESTAMP,
                    CONSTRAINT fk_product_stocks_product FOREIGN KEY (product_id) 
                        REFERENCES products(id) ON DELETE CASCADE,
                    CONSTRAINT ck_quantity_nonnegative CHECK (quantity >= 0),
                    CONSTRAINT ck_reserved_nonnegative CHECK (reserved >= 0),
                    CONSTRAINT ck_reserved_quantity CHECK (reserved <= quantity)
                );

                CREATE INDEX IF NOT EXISTS idx_products_sku ON products(sku);
                CREATE INDEX IF NOT EXISTS idx_products_category ON products(category);
                CREATE INDEX IF NOT EXISTS idx_stocks_product ON product_stocks(product_id);
            ";

            await _connection.ExecuteAsync(sql);
        }
    }
}