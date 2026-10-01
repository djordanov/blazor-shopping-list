using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingListApp.Migrations
{
    /// <inheritdoc />
    public partial class ImproveListItemModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListItem_ItemId",
                table: "ListItem");

            migrationBuilder.DropColumn(
                name: "IsPurchased",
                table: "ListItem");

            migrationBuilder.AddColumn<int>(
                name: "Count",
                table: "ListItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ListItem_ItemId",
                table: "ListItem",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListItem_ItemId",
                table: "ListItem");

            migrationBuilder.DropColumn(
                name: "Count",
                table: "ListItem");

            migrationBuilder.AddColumn<bool>(
                name: "IsPurchased",
                table: "ListItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ListItem_ItemId",
                table: "ListItem",
                column: "ItemId",
                unique: true);
        }
    }
}
