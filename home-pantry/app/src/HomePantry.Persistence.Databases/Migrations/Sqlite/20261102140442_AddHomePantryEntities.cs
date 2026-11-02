using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomePantry.Persistence.Databases.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddHomePantryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dict_Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsFolder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFreezer = table.Column<bool>(type: "INTEGER", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModifierUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Import_Origin = table.Column<int>(type: "INTEGER", nullable: true),
                    Import_Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_SourceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Import_ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Depth = table.Column<int>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dict_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Locations_Dict_Locations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Servings = table.Column<int>(type: "INTEGER", nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModifierUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Import_Origin = table.Column<int>(type: "INTEGER", nullable: true),
                    Import_Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_SourceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Import_ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dict_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModifierUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Import_Origin = table.Column<int>(type: "INTEGER", nullable: true),
                    Import_Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_SourceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Import_ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dict_Stores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModifierUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Import_Origin = table.Column<int>(type: "INTEGER", nullable: true),
                    Import_Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_SourceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Import_ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsFolder = table.Column<int>(type: "INTEGER", nullable: false),
                    Unit = table.Column<int>(type: "INTEGER", nullable: true),
                    Barcode = table.Column<string>(type: "TEXT", nullable: true),
                    MinimumStock = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: true),
                    DefaultLocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DefaultShelfLifeDays = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModifierUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Import_Origin = table.Column<int>(type: "INTEGER", nullable: true),
                    Import_Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Import_SourceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Import_ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Depth = table.Column<int>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dict_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Products_Dict_Locations_DefaultLocationId",
                        column: x => x.DefaultLocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dict_Products_Dict_Products_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Consumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Consumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Consumes_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_CookRecipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecipeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Servings = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_CookRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_CookRecipes_Dict_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_CookRecipes_Dict_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Dict_Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_CookRecipes_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_InventoryCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_InventoryCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_InventoryCounts_Dict_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_InventoryCounts_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StoreId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Purchases_Dict_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Dict_Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Purchases_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_ShoppingLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StoreId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_ShoppingLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_ShoppingLists_Dict_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Dict_Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_ShoppingLists_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Transfers_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dict_RecipeLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecipeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dict_RecipeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_RecipeLines_Dict_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dict_RecipeLines_Dict_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Dict_Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reg_LastPrice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_StoreId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Details_Price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_LastPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_LastPrice_Dict_Products_Dimensions_ProductId",
                        column: x => x.Dimensions_ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_LastPrice_Dict_Stores_Dimensions_StoreId",
                        column: x => x.Dimensions_StoreId,
                        principalTable: "Dict_Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_LastPrice_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_Stock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_BestBeforeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Resources_Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Resources_Value = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_Stock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_Stock_Dict_Locations_Dimensions_LocationId",
                        column: x => x.Dimensions_LocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_Stock_Dict_Products_Dimensions_ProductId",
                        column: x => x.Dimensions_ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_Stock_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_Stock_Balance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_BestBeforeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Resources_Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Resources_Value = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_Stock_Balance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_Stock_Balance_Dict_Locations_Dimensions_LocationId",
                        column: x => x.Dimensions_LocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_Stock_Balance_Dict_Products_Dimensions_ProductId",
                        column: x => x.Dimensions_ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_ConsumeLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConsumeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_ConsumeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_ConsumeLines_Dict_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_ConsumeLines_Dict_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_ConsumeLines_Doc_Consumes_ConsumeId",
                        column: x => x.ConsumeId,
                        principalTable: "Doc_Consumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_CookRecipeLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CookRecipeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_CookRecipeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_CookRecipeLines_Dict_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_CookRecipeLines_Doc_CookRecipes_CookRecipeId",
                        column: x => x.CookRecipeId,
                        principalTable: "Doc_CookRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_InventoryCountLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryCountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_InventoryCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_InventoryCountLines_Dict_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_InventoryCountLines_Doc_InventoryCounts_InventoryCountId",
                        column: x => x.InventoryCountId,
                        principalTable: "Doc_InventoryCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_PurchaseLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BestBeforeDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_PurchaseLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_PurchaseLines_Dict_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_PurchaseLines_Dict_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_PurchaseLines_Doc_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Doc_Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_ShoppingListLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShoppingListId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Bought = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_ShoppingListLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_ShoppingListLines_Dict_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_ShoppingListLines_Doc_ShoppingLists_ShoppingListId",
                        column: x => x.ShoppingListId,
                        principalTable: "Doc_ShoppingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_TransferLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransferId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToLocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_TransferLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_TransferLines_Dict_Locations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_TransferLines_Dict_Locations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_TransferLines_Dict_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Dict_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_TransferLines_Doc_Transfers_TransferId",
                        column: x => x.TransferId,
                        principalTable: "Doc_Transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Locations_Code",
                table: "Dict_Locations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Locations_Import_Source_Import_Key",
                table: "Dict_Locations",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Locations_ParentId_Name",
                table: "Dict_Locations",
                columns: new[] { "ParentId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Locations_Path",
                table: "Dict_Locations",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Products_Code",
                table: "Dict_Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Products_DefaultLocationId",
                table: "Dict_Products",
                column: "DefaultLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Products_Import_Source_Import_Key",
                table: "Dict_Products",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Products_ParentId_Name",
                table: "Dict_Products",
                columns: new[] { "ParentId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Products_Path",
                table: "Dict_Products",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_RecipeLines_ProductId",
                table: "Dict_RecipeLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_RecipeLines_RecipeId",
                table: "Dict_RecipeLines",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Recipes_Code",
                table: "Dict_Recipes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Recipes_Import_Source_Import_Key",
                table: "Dict_Recipes",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Stores_Code",
                table: "Dict_Stores",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Stores_Import_Source_Import_Key",
                table: "Dict_Stores",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ConsumeLines_ConsumeId",
                table: "Doc_ConsumeLines",
                column: "ConsumeId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ConsumeLines_LocationId",
                table: "Doc_ConsumeLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ConsumeLines_ProductId",
                table: "Doc_ConsumeLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_CookRecipeLines_CookRecipeId",
                table: "Doc_CookRecipeLines",
                column: "CookRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_CookRecipeLines_ProductId",
                table: "Doc_CookRecipeLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_CookRecipes_LocationId",
                table: "Doc_CookRecipes",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_CookRecipes_RecipeId",
                table: "Doc_CookRecipes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Documents_DocumentTypeId",
                table: "Doc_Documents",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Documents_Import_Source_Import_Key",
                table: "Doc_Documents",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doc_InventoryCountLines_InventoryCountId",
                table: "Doc_InventoryCountLines",
                column: "InventoryCountId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_InventoryCountLines_ProductId",
                table: "Doc_InventoryCountLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_InventoryCounts_LocationId",
                table: "Doc_InventoryCounts",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_PurchaseLines_LocationId",
                table: "Doc_PurchaseLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_PurchaseLines_ProductId",
                table: "Doc_PurchaseLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_PurchaseLines_PurchaseId",
                table: "Doc_PurchaseLines",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Purchases_StoreId",
                table: "Doc_Purchases",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ShoppingListLines_ProductId",
                table: "Doc_ShoppingListLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ShoppingListLines_ShoppingListId",
                table: "Doc_ShoppingListLines",
                column: "ShoppingListId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ShoppingLists_StoreId",
                table: "Doc_ShoppingLists",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_TransferLines_FromLocationId",
                table: "Doc_TransferLines",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_TransferLines_ProductId",
                table: "Doc_TransferLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_TransferLines_ToLocationId",
                table: "Doc_TransferLines",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_TransferLines_TransferId",
                table: "Doc_TransferLines",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_LastPrice_Dimensions_ProductId",
                table: "Reg_LastPrice",
                column: "Dimensions_ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_LastPrice_Dimensions_StoreId",
                table: "Reg_LastPrice",
                column: "Dimensions_StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_LastPrice_DocumentId_LineId",
                table: "Reg_LastPrice",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_LastPrice_Period",
                table: "Reg_LastPrice",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Stock_Dimensions_LocationId",
                table: "Reg_Stock",
                column: "Dimensions_LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Stock_Dimensions_ProductId",
                table: "Reg_Stock",
                column: "Dimensions_ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Stock_DocumentId_LineId_IsExpense",
                table: "Reg_Stock",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Stock_Period",
                table: "Reg_Stock",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Stock_Balance_Dimensions_LocationId",
                table: "Reg_Stock_Balance",
                column: "Dimensions_LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Stock_Balance_Dimensions_ProductId_Dimensions_LocationId_Dimensions_BestBeforeDate",
                table: "Reg_Stock_Balance",
                columns: new[] { "Dimensions_ProductId", "Dimensions_LocationId", "Dimensions_BestBeforeDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dict_RecipeLines");

            migrationBuilder.DropTable(
                name: "Doc_ConsumeLines");

            migrationBuilder.DropTable(
                name: "Doc_CookRecipeLines");

            migrationBuilder.DropTable(
                name: "Doc_InventoryCountLines");

            migrationBuilder.DropTable(
                name: "Doc_PurchaseLines");

            migrationBuilder.DropTable(
                name: "Doc_ShoppingListLines");

            migrationBuilder.DropTable(
                name: "Doc_TransferLines");

            migrationBuilder.DropTable(
                name: "Reg_LastPrice");

            migrationBuilder.DropTable(
                name: "Reg_Stock");

            migrationBuilder.DropTable(
                name: "Reg_Stock_Balance");

            migrationBuilder.DropTable(
                name: "Doc_Consumes");

            migrationBuilder.DropTable(
                name: "Doc_CookRecipes");

            migrationBuilder.DropTable(
                name: "Doc_InventoryCounts");

            migrationBuilder.DropTable(
                name: "Doc_Purchases");

            migrationBuilder.DropTable(
                name: "Doc_ShoppingLists");

            migrationBuilder.DropTable(
                name: "Doc_Transfers");

            migrationBuilder.DropTable(
                name: "Dict_Products");

            migrationBuilder.DropTable(
                name: "Dict_Recipes");

            migrationBuilder.DropTable(
                name: "Dict_Stores");

            migrationBuilder.DropTable(
                name: "Doc_Documents");

            migrationBuilder.DropTable(
                name: "Dict_Locations");
        }
    }
}
