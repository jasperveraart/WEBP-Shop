using Microsoft.EntityFrameworkCore.Migrations;
using PWebShop.Infrastructure;

#nullable disable

namespace PWebShop.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20250309000000_RemovePriceStockPaymentShipment")]
public partial class RemovePriceStockPaymentShipment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Payments");

        migrationBuilder.DropTable(
            name: "Prices");

        migrationBuilder.DropTable(
            name: "Shipments");

        migrationBuilder.DropTable(
            name: "Stocks");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Prices",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                MarkupPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                FinalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Prices", x => x.Id);
                table.ForeignKey(
                    name: "FK_Prices_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Stocks",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                QuantityAvailable = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stocks", x => x.Id);
                table.ForeignKey(
                    name: "FK_Stocks_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                PaidAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payments_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Shipments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                Carrier = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                TrackingCode = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                ShippedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                DeliveredAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Shipments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Shipments_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Payments_OrderId",
            table: "Payments",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Prices_ProductId",
            table: "Prices",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_Shipments_OrderId",
            table: "Shipments",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Stocks_ProductId",
            table: "Stocks",
            column: "ProductId",
            unique: true);
    }
}
