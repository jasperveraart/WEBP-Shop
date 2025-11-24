using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWebShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductStatusToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "PendingApproval");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Products");
        }
    }
}
