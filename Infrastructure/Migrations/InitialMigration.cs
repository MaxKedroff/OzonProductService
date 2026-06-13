using FluentMigrator;

namespace Infrastructure.Migrations
{
    [Migration(20240101001)]
    public class InitialMigration : Migration
    {
        public override void Up()
        {
            Create.Table("products")
                .WithColumn("id").AsGuid().PrimaryKey()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("description").AsString(2000).Nullable()
                .WithColumn("price").AsDecimal(12, 2).NotNullable()
                .WithColumn("currency").AsString(3).NotNullable().WithDefaultValue("RUB")
                .WithColumn("sku").AsString(50).NotNullable().Unique()
                .WithColumn("category").AsString(100).NotNullable()
                .WithColumn("is_deleted").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Table("product_stocks")
                .WithColumn("id").AsGuid().PrimaryKey()
                .WithColumn("product_id").AsGuid().NotNullable().ForeignKey("products", "id")
                .WithColumn("quantity").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("reserved").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("warehouse").AsString(100).NotNullable().WithDefaultValue("main")
                .WithColumn("lead_time_days").AsInt32().NotNullable().WithDefaultValue(3)
                .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Index("idx_products_sku").OnTable("products").OnColumn("sku").Unique();
            Create.Index("idx_products_category").OnTable("products").OnColumn("category");
            Create.Index("idx_stocks_product").OnTable("product_stocks").OnColumn("product_id").Unique();
        }

        public override void Down()
        {
            Delete.Table("product_stocks");
            Delete.Table("products");
        }
    }
}
