using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomePantry.Persistence.Databases.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddPurchaseShoppingListAndDecimalServings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShoppingListId",
                table: "Doc_Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Servings",
                table: "Doc_CookRecipes",
                type: "TEXT",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Purchases_ShoppingListId",
                table: "Doc_Purchases",
                column: "ShoppingListId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doc_Purchases_Doc_ShoppingLists_ShoppingListId",
                table: "Doc_Purchases",
                column: "ShoppingListId",
                principalTable: "Doc_ShoppingLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doc_Purchases_Doc_ShoppingLists_ShoppingListId",
                table: "Doc_Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Doc_Purchases_ShoppingListId",
                table: "Doc_Purchases");

            migrationBuilder.DropColumn(
                name: "ShoppingListId",
                table: "Doc_Purchases");

            migrationBuilder.AlterColumn<int>(
                name: "Servings",
                table: "Doc_CookRecipes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 14,
                oldScale: 2);
        }
    }
}
