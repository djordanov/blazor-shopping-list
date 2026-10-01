using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingListApp.Migrations
{
    /// <inheritdoc />
    public partial class RenameListItemTableToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListItem_Section_SectionId",
                table: "ListItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ListItem_ShoppingList_ShoppingListId",
                table: "ListItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ListItem",
                table: "ListItem");

            migrationBuilder.RenameTable(
                name: "ListItem",
                newName: "Item");

            migrationBuilder.RenameIndex(
                name: "IX_ListItem_ShoppingListId",
                table: "Item",
                newName: "IX_Item_ShoppingListId");

            migrationBuilder.RenameIndex(
                name: "IX_ListItem_SectionId",
                table: "Item",
                newName: "IX_Item_SectionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Item",
                table: "Item",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Section_SectionId",
                table: "Item",
                column: "SectionId",
                principalTable: "Section",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_ShoppingList_ShoppingListId",
                table: "Item",
                column: "ShoppingListId",
                principalTable: "ShoppingList",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Item_Section_SectionId",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_ShoppingList_ShoppingListId",
                table: "Item");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Item",
                table: "Item");

            migrationBuilder.RenameTable(
                name: "Item",
                newName: "ListItem");

            migrationBuilder.RenameIndex(
                name: "IX_Item_ShoppingListId",
                table: "ListItem",
                newName: "IX_ListItem_ShoppingListId");

            migrationBuilder.RenameIndex(
                name: "IX_Item_SectionId",
                table: "ListItem",
                newName: "IX_ListItem_SectionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ListItem",
                table: "ListItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ListItem_Section_SectionId",
                table: "ListItem",
                column: "SectionId",
                principalTable: "Section",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ListItem_ShoppingList_ShoppingListId",
                table: "ListItem",
                column: "ShoppingListId",
                principalTable: "ShoppingList",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
