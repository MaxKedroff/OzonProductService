using FluentMigrator;

namespace Infrastructure.Migrations
{
    [Migration(20240101001)]
    public class CreateProductsTable : Migration
    {
        public override void Down()
        {
            Delete.Table("products");
        }

        public override void Up()
        {
            Create.Table("products")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(2000).Nullable()
            .WithColumn("price").AsDecimal(12, 2).NotNullable()
            .WithColumn("currency").AsString(3).NotNullable().WithDefaultValue("USD")
            .WithColumn("sku").AsString(50).NotNullable().Unique()
            .WithColumn("category").AsString(100).NotNullable()
            .WithColumn("is_deleted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Index("idx_products_category").OnTable("products").OnColumn("category");
            Create.Index("idx_products_name").OnTable("products").OnColumn("name").Ascending();
            Create.Index("idx_products_sku").OnTable("products").OnColumn("sku").Unique();
        }
    }
}
