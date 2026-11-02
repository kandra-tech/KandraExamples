using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomePantry.Persistence.Databases.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddHouseholdPartB : Migration
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
                name: "IX_Doc_MealPlanLines_MealPlanId",
                table: "Doc_MealPlanLines",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_MealPlanLines_RecipeId",
                table: "Doc_MealPlanLines",
                column: "RecipeId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dict_Equipment");

            migrationBuilder.DropTable(
                name: "Doc_BatteriesCharged");

            migrationBuilder.DropTable(
                name: "Doc_ChoresDone");

            migrationBuilder.DropTable(
                name: "Doc_MealPlanLines");

            migrationBuilder.DropTable(
                name: "Reg_BatteryCycles");

            migrationBuilder.DropTable(
                name: "Reg_BatteryLog");

            migrationBuilder.DropTable(
                name: "Reg_ChoreLog");

            migrationBuilder.DropTable(
                name: "Reg_PlannedMeals");

            migrationBuilder.DropTable(
                name: "Doc_MealPlans");

            migrationBuilder.DropTable(
                name: "Dict_Batteries");

            migrationBuilder.DropTable(
                name: "Dict_Chores");

            migrationBuilder.DropTable(
                name: "Dict_HouseholdMembers");
        }
    }
}
