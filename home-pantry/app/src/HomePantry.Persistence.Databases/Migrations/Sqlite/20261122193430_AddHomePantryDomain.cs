using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomePantry.Persistence.Databases.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddHomePantryDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dict_Batteries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsedIn = table.Column<string>(type: "TEXT", nullable: false),
                    ChargeIntervalDays = table.Column<int>(type: "INTEGER", nullable: false),
                    BatteryKind = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Dict_Batteries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dict_HouseholdMembers",
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
                    table.PrimaryKey("PK_Dict_HouseholdMembers", x => x.Id);
                });

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
                name: "Dict_Chores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Recurrence = table.Column<int>(type: "INTEGER", nullable: false),
                    EveryNDays = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Dict_Chores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Chores_Dict_HouseholdMembers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Dict_HouseholdMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BoughtOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WarrantyUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ManualFileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PhotoFileId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Dict_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Equipment_Dict_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Dict_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "Doc_BatteriesCharged",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BatteryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Replaced = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_BatteriesCharged", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_BatteriesCharged_Dict_Batteries_BatteryId",
                        column: x => x.BatteryId,
                        principalTable: "Dict_Batteries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_BatteriesCharged_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Servings = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
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
                name: "Doc_MealPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_MealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_MealPlans_Doc_Documents_Id",
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
                name: "Reg_BatteryCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_BatteryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_Cycles = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Resources_Replacements = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_BatteryCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_BatteryCycles_Dict_Batteries_Dimensions_BatteryId",
                        column: x => x.Dimensions_BatteryId,
                        principalTable: "Dict_Batteries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_BatteryCycles_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_BatteryLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_BatteryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Details_Replaced = table.Column<bool>(type: "INTEGER", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_BatteryLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_BatteryLog_Dict_Batteries_Dimensions_BatteryId",
                        column: x => x.Dimensions_BatteryId,
                        principalTable: "Dict_Batteries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_BatteryLog_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_PlannedMeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Dimensions_RecipeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Details_Servings = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_PlannedMeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_PlannedMeals_Dict_Recipes_Dimensions_RecipeId",
                        column: x => x.Dimensions_RecipeId,
                        principalTable: "Dict_Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_PlannedMeals_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_ChoresDone",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChoreId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DoneById = table.Column<Guid>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_ChoresDone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_ChoresDone_Dict_Chores_ChoreId",
                        column: x => x.ChoreId,
                        principalTable: "Dict_Chores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_ChoresDone_Dict_HouseholdMembers_DoneById",
                        column: x => x.DoneById,
                        principalTable: "Dict_HouseholdMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_ChoresDone_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reg_ChoreLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ChoreId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Details_DoneById = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_ChoreLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_ChoreLog_Dict_Chores_Dimensions_ChoreId",
                        column: x => x.Dimensions_ChoreId,
                        principalTable: "Dict_Chores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_ChoreLog_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "Doc_MealPlanLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MealPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Servings = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_MealPlanLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_MealPlanLines_Dict_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Dict_Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_MealPlanLines_Doc_MealPlans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalTable: "Doc_MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StoreId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShoppingListId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Doc_Purchases_Doc_ShoppingLists_ShoppingListId",
                        column: x => x.ShoppingListId,
                        principalTable: "Doc_ShoppingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Batteries_Code",
                table: "Dict_Batteries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Batteries_Import_Source_Import_Key",
                table: "Dict_Batteries",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Chores_AssignedToId",
                table: "Dict_Chores",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Chores_Code",
                table: "Dict_Chores",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Chores_Import_Source_Import_Key",
                table: "Dict_Chores",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Equipment_Code",
                table: "Dict_Equipment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Equipment_Import_Source_Import_Key",
                table: "Dict_Equipment",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Equipment_LocationId",
                table: "Dict_Equipment",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_HouseholdMembers_Code",
                table: "Dict_HouseholdMembers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_HouseholdMembers_Import_Source_Import_Key",
                table: "Dict_HouseholdMembers",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

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
                name: "IX_Doc_BatteriesCharged_BatteryId",
                table: "Doc_BatteriesCharged",
                column: "BatteryId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ChoresDone_ChoreId",
                table: "Doc_ChoresDone",
                column: "ChoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ChoresDone_DoneById",
                table: "Doc_ChoresDone",
                column: "DoneById");

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
                name: "IX_Doc_MealPlanLines_MealPlanId",
                table: "Doc_MealPlanLines",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_MealPlanLines_RecipeId",
                table: "Doc_MealPlanLines",
                column: "RecipeId");

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
                name: "IX_Doc_Purchases_ShoppingListId",
                table: "Doc_Purchases",
                column: "ShoppingListId");

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
                name: "IX_Reg_BatteryCycles_Dimensions_BatteryId",
                table: "Reg_BatteryCycles",
                column: "Dimensions_BatteryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_BatteryCycles_DocumentId_LineId_IsExpense",
                table: "Reg_BatteryCycles",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_BatteryCycles_Period",
                table: "Reg_BatteryCycles",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_BatteryLog_Dimensions_BatteryId",
                table: "Reg_BatteryLog",
                column: "Dimensions_BatteryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_BatteryLog_DocumentId_LineId",
                table: "Reg_BatteryLog",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_BatteryLog_Period",
                table: "Reg_BatteryLog",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ChoreLog_Dimensions_ChoreId",
                table: "Reg_ChoreLog",
                column: "Dimensions_ChoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ChoreLog_DocumentId_LineId",
                table: "Reg_ChoreLog",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ChoreLog_Period",
                table: "Reg_ChoreLog",
                column: "Period");

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
                name: "IX_Reg_PlannedMeals_Dimensions_RecipeId",
                table: "Reg_PlannedMeals",
                column: "Dimensions_RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_PlannedMeals_DocumentId_LineId",
                table: "Reg_PlannedMeals",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_PlannedMeals_Period",
                table: "Reg_PlannedMeals",
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
                name: "Dict_Equipment");

            migrationBuilder.DropTable(
                name: "Dict_RecipeLines");

            migrationBuilder.DropTable(
                name: "Doc_BatteriesCharged");

            migrationBuilder.DropTable(
                name: "Doc_ChoresDone");

            migrationBuilder.DropTable(
                name: "Doc_ConsumeLines");

            migrationBuilder.DropTable(
                name: "Doc_CookRecipeLines");

            migrationBuilder.DropTable(
                name: "Doc_InventoryCountLines");

            migrationBuilder.DropTable(
                name: "Doc_MealPlanLines");

            migrationBuilder.DropTable(
                name: "Doc_PurchaseLines");

            migrationBuilder.DropTable(
                name: "Doc_ShoppingListLines");

            migrationBuilder.DropTable(
                name: "Doc_TransferLines");

            migrationBuilder.DropTable(
                name: "Reg_BatteryCycles");

            migrationBuilder.DropTable(
                name: "Reg_BatteryLog");

            migrationBuilder.DropTable(
                name: "Reg_ChoreLog");

            migrationBuilder.DropTable(
                name: "Reg_LastPrice");

            migrationBuilder.DropTable(
                name: "Reg_PlannedMeals");

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
                name: "Doc_MealPlans");

            migrationBuilder.DropTable(
                name: "Doc_Purchases");

            migrationBuilder.DropTable(
                name: "Doc_Transfers");

            migrationBuilder.DropTable(
                name: "Dict_Batteries");

            migrationBuilder.DropTable(
                name: "Dict_Chores");

            migrationBuilder.DropTable(
                name: "Dict_Products");

            migrationBuilder.DropTable(
                name: "Dict_Recipes");

            migrationBuilder.DropTable(
                name: "Doc_ShoppingLists");

            migrationBuilder.DropTable(
                name: "Dict_HouseholdMembers");

            migrationBuilder.DropTable(
                name: "Dict_Locations");

            migrationBuilder.DropTable(
                name: "Dict_Stores");

            migrationBuilder.DropTable(
                name: "Doc_Documents");
        }
    }
}
