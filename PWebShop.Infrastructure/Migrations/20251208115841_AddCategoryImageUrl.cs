using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWebShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryImageUrl : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Categories");
        }
    }
}
