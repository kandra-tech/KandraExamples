using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAccounting.Persistence.Databases.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddPersonalAccountingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dict_Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFolder = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Dict_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Categories_Dict_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Dict_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Currencies",
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
                    table.PrimaryKey("PK_Dict_Currencies", x => x.Id);
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
                name: "Dict_Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_Dict_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Accounts_Dict_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Dict_Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Budget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Budget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Budget_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_OpeningBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_OpeningBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_OpeningBalances_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_SetExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_SetExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_SetExchangeRates_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reg_BudgetPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Details_PlannedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_BudgetPlan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_BudgetPlan_Dict_Categories_Dimensions_CategoryId",
                        column: x => x.Dimensions_CategoryId,
                        principalTable: "Dict_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_BudgetPlan_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Details_Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_ExchangeRates_Dict_Currencies_Dimensions_CurrencyId",
                        column: x => x.Dimensions_CurrencyId,
                        principalTable: "Dict_Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_ExchangeRates_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Expense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Expense", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Expense_Dict_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Dict_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Expense_Dict_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Dict_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Expense_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Income",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Income", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Income_Dict_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Dict_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Income_Dict_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Dict_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Income_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Transfer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AmountOut = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AmountIn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Transfer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Transfer_Dict_Accounts_FromAccountId",
                        column: x => x.FromAccountId,
                        principalTable: "Dict_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Transfer_Dict_Accounts_ToAccountId",
                        column: x => x.ToAccountId,
                        principalTable: "Dict_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Transfer_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reg_AccountBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Details_CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Details_CounterAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Details_Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_AccountBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_AccountBalances_Dict_Accounts_Dimensions_AccountId",
                        column: x => x.Dimensions_AccountId,
                        principalTable: "Dict_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_AccountBalances_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_AccountBalances_Balance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_AccountBalances_Balance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_AccountBalances_Balance_Dict_Accounts_Dimensions_AccountId",
                        column: x => x.Dimensions_AccountId,
                        principalTable: "Dict_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_CategoryTurnover",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_AmountInAccountCurrency = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Resources_AmountInBaseCurrency = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Details_Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_CategoryTurnover", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_CategoryTurnover_Dict_Accounts_Dimensions_AccountId",
                        column: x => x.Dimensions_AccountId,
                        principalTable: "Dict_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_CategoryTurnover_Dict_Categories_Dimensions_CategoryId",
                        column: x => x.Dimensions_CategoryId,
                        principalTable: "Dict_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_CategoryTurnover_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_BudgetLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BudgetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlannedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_BudgetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_BudgetLines_Dict_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Dict_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_BudgetLines_Doc_Budget_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Doc_Budget",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_OpeningBalancesLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpeningBalancesId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_OpeningBalancesLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_OpeningBalancesLines_Dict_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Dict_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_OpeningBalancesLines_Doc_OpeningBalances_OpeningBalancesId",
                        column: x => x.OpeningBalancesId,
                        principalTable: "Doc_OpeningBalances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_SetExchangeRatesLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SetExchangeRatesId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_SetExchangeRatesLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_SetExchangeRatesLines_Dict_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Dict_Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_SetExchangeRatesLines_Doc_SetExchangeRates_SetExchangeRatesId",
                        column: x => x.SetExchangeRatesId,
                        principalTable: "Doc_SetExchangeRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Accounts_Code",
                table: "Dict_Accounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Accounts_CurrencyId",
                table: "Dict_Accounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Accounts_Import_Source_Import_Key",
                table: "Dict_Accounts",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Categories_Code",
                table: "Dict_Categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Categories_Import_Source_Import_Key",
                table: "Dict_Categories",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Categories_ParentId_Name",
                table: "Dict_Categories",
                columns: new[] { "ParentId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Categories_Path",
                table: "Dict_Categories",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Currencies_Code",
                table: "Dict_Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Currencies_Import_Source_Import_Key",
                table: "Dict_Currencies",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doc_BudgetLines_BudgetId",
                table: "Doc_BudgetLines",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_BudgetLines_CategoryId",
                table: "Doc_BudgetLines",
                column: "CategoryId");

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
                name: "IX_Doc_Expense_AccountId",
                table: "Doc_Expense",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Expense_CategoryId",
                table: "Doc_Expense",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Income_AccountId",
                table: "Doc_Income",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Income_CategoryId",
                table: "Doc_Income",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_OpeningBalancesLines_AccountId",
                table: "Doc_OpeningBalancesLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_OpeningBalancesLines_OpeningBalancesId",
                table: "Doc_OpeningBalancesLines",
                column: "OpeningBalancesId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_SetExchangeRatesLines_CurrencyId",
                table: "Doc_SetExchangeRatesLines",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_SetExchangeRatesLines_SetExchangeRatesId",
                table: "Doc_SetExchangeRatesLines",
                column: "SetExchangeRatesId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Transfer_FromAccountId",
                table: "Doc_Transfer",
                column: "FromAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Transfer_ToAccountId",
                table: "Doc_Transfer",
                column: "ToAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_AccountBalances_Dimensions_AccountId",
                table: "Reg_AccountBalances",
                column: "Dimensions_AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_AccountBalances_DocumentId_LineId_IsExpense",
                table: "Reg_AccountBalances",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_AccountBalances_Period",
                table: "Reg_AccountBalances",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_AccountBalances_Balance_Dimensions_AccountId",
                table: "Reg_AccountBalances_Balance",
                column: "Dimensions_AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_BudgetPlan_Dimensions_CategoryId",
                table: "Reg_BudgetPlan",
                column: "Dimensions_CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_BudgetPlan_DocumentId_LineId",
                table: "Reg_BudgetPlan",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_BudgetPlan_Period",
                table: "Reg_BudgetPlan",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_CategoryTurnover_Dimensions_AccountId",
                table: "Reg_CategoryTurnover",
                column: "Dimensions_AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_CategoryTurnover_Dimensions_CategoryId",
                table: "Reg_CategoryTurnover",
                column: "Dimensions_CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_CategoryTurnover_DocumentId_LineId_IsExpense",
                table: "Reg_CategoryTurnover",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_CategoryTurnover_Period",
                table: "Reg_CategoryTurnover",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ExchangeRates_Dimensions_CurrencyId",
                table: "Reg_ExchangeRates",
                column: "Dimensions_CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ExchangeRates_DocumentId_LineId",
                table: "Reg_ExchangeRates",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ExchangeRates_Period",
                table: "Reg_ExchangeRates",
                column: "Period");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Doc_BudgetLines");

            migrationBuilder.DropTable(
                name: "Doc_Expense");

            migrationBuilder.DropTable(
                name: "Doc_Income");

            migrationBuilder.DropTable(
                name: "Doc_OpeningBalancesLines");

            migrationBuilder.DropTable(
                name: "Doc_SetExchangeRatesLines");

            migrationBuilder.DropTable(
                name: "Doc_Transfer");

            migrationBuilder.DropTable(
                name: "Reg_AccountBalances");

            migrationBuilder.DropTable(
                name: "Reg_AccountBalances_Balance");

            migrationBuilder.DropTable(
                name: "Reg_BudgetPlan");

            migrationBuilder.DropTable(
                name: "Reg_CategoryTurnover");

            migrationBuilder.DropTable(
                name: "Reg_ExchangeRates");

            migrationBuilder.DropTable(
                name: "Doc_Budget");

            migrationBuilder.DropTable(
                name: "Doc_OpeningBalances");

            migrationBuilder.DropTable(
                name: "Doc_SetExchangeRates");

            migrationBuilder.DropTable(
                name: "Dict_Accounts");

            migrationBuilder.DropTable(
                name: "Dict_Categories");

            migrationBuilder.DropTable(
                name: "Doc_Documents");

            migrationBuilder.DropTable(
                name: "Dict_Currencies");
        }
    }
}
