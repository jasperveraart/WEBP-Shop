using Microsoft.EntityFrameworkCore.Migrations;
using PWebShop.Infrastructure;

#nullable disable

namespace PWebShop.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20250313000001_UpdateProductSupplierAndMarkup")]
public partial class UpdateProductSupplierAndMarkup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Status",
            table: "Products");

        migrationBuilder.AddColumn<double>(
            name: "MarkupPercentage",
            table: "Products",
            type: "float",
            nullable: false,
            defaultValue: 0.0);

        migrationBuilder.AddColumn<string>(
            name: "SupplierIdTemp",
            table: "Products",
            type: "nvarchar(450)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql("UPDATE Products SET SupplierIdTemp = CAST(SupplierId AS nvarchar(450))");

        migrationBuilder.DropColumn(
            name: "SupplierId",
            table: "Products");

        migrationBuilder.RenameColumn(
            name: "SupplierIdTemp",
            table: "Products",
            newName: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_Products_SupplierId",
            table: "Products",
            column: "SupplierId");

        migrationBuilder.AddForeignKey(
            name: "FK_Products_AspNetUsers_SupplierId",
            table: "Products",
            column: "SupplierId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Products_AspNetUsers_SupplierId",
            table: "Products");

        migrationBuilder.DropIndex(
            name: "IX_Products_SupplierId",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "MarkupPercentage",
            table: "Products");

        migrationBuilder.AddColumn<int>(
            name: "SupplierIdTemp",
            table: "Products",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("UPDATE Products SET SupplierIdTemp = TRY_CAST(SupplierId AS int)");

        migrationBuilder.DropColumn(
            name: "SupplierId",
            table: "Products");

        migrationBuilder.RenameColumn(
            name: "SupplierIdTemp",
            table: "Products",
            newName: "SupplierId");

        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "Products",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");
    }
}
