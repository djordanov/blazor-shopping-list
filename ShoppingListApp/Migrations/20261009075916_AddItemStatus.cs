using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingListApp.Migrations
{
    /// <inheritdoc />
    public partial class AddItemStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCollected",
                table: "Item");

            migrationBuilder.AddColumn<int>(
                name: "ItemStatus",
                table: "Item",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemStatus",
                table: "Item");

            migrationBuilder.AddColumn<bool>(
                name: "IsCollected",
                table: "Item",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
