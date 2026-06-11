using FluentMigrator;

namespace Infrastructure.Migrations
{

    [Migration(20240101002)]
    public class CreateProductStocksTable : Migration
    {
        public override void Down()
        {
            Delete.Table("product_stocks");

        }

        public override void Up()
        {
            Create.Table("product_stocks")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("product_id").AsGuid().NotNullable().ForeignKey("products", "id")
            .WithColumn("quantity").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("reserved").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("warehouse").AsString(100).NotNullable().WithDefaultValue("main")
            .WithColumn("lead_time_days").AsInt32().NotNullable().WithDefaultValue(3)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Index("idx_stocks_product").OnTable("product_stocks").OnColumn("product_id").Unique();
            Create.Index("idx_stocks_quantity").OnTable("product_stocks").OnColumn("quantity");
        }
    }
}
