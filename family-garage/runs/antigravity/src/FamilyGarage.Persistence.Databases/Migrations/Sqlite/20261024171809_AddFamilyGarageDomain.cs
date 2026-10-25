using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyGarage.Persistence.Databases.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddFamilyGarageDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dict_Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsFolder = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    ServiceIntervalKm = table.Column<int>(type: "INTEGER", nullable: true),
                    ServiceIntervalMonths = table.Column<int>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_Dict_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Items_Dict_Items_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Dict_Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Dict_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Make = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    PlateNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Vin = table.Column<string>(type: "TEXT", nullable: false),
                    FuelType = table.Column<int>(type: "INTEGER", nullable: false),
                    Photo = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Dict_Vehicles", x => x.Id);
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
                name: "Dict_FinanceContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    LenderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SignedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AnnualInterestRatePercent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    TermMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyPayment = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaymentDayOfMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Dict_FinanceContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_FinanceContracts_Dict_Providers_LenderId",
                        column: x => x.LenderId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dict_FinanceContracts_Dict_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Accidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Odometer = table.Column<int>(type: "INTEGER", nullable: false),
                    Place = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EstimatedDamage = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Photo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Accidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Accidents_Dict_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Accidents_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    ScannedDocument = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Policies_Dict_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Policies_Dict_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Policies_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Refuels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuelStationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Litres = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Odometer = table.Column<int>(type: "INTEGER", nullable: false),
                    FullTank = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Refuels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Refuels_Dict_Providers_FuelStationId",
                        column: x => x.FuelStationId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Refuels_Dict_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Refuels_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_VehicleSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Buyer = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Odometer = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_VehicleSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_VehicleSales_Dict_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_VehicleSales_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reg_LastDone",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Details_Odometer = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_LastDone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_LastDone_Dict_Items_Dimensions_ItemId",
                        column: x => x.Dimensions_ItemId,
                        principalTable: "Dict_Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_LastDone_Dict_Vehicles_Dimensions_VehicleId",
                        column: x => x.Dimensions_VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_LastDone_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_Odometer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Details_Kilometres = table.Column<int>(type: "INTEGER", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_Odometer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_Odometer_Dict_Vehicles_Dimensions_VehicleId",
                        column: x => x.Dimensions_VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_Odometer_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_PolicyValidity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_PolicyKind = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_ValidTo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_PolicyValidity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_PolicyValidity_Dict_Vehicles_Dimensions_VehicleId",
                        column: x => x.Dimensions_VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_PolicyValidity_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_ServiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Details_Odometer = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_ServiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_ServiceLines_Dict_Items_Dimensions_ItemId",
                        column: x => x.Dimensions_ItemId,
                        principalTable: "Dict_Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_ServiceLines_Dict_Vehicles_Dimensions_VehicleId",
                        column: x => x.Dimensions_VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_ServiceLines_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_VehicleCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Details_Litres = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Details_FullTank = table.Column<bool>(type: "INTEGER", nullable: true),
                    Details_Odometer = table.Column<int>(type: "INTEGER", nullable: true),
                    Details_IsSale = table.Column<bool>(type: "INTEGER", nullable: false),
                    Details_Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_VehicleCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_VehicleCosts_Dict_Vehicles_Dimensions_VehicleId",
                        column: x => x.Dimensions_VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_VehicleCosts_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_FinancePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinanceContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Principal = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Interest = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Fees = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_FinancePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_FinancePayments_Dict_FinanceContracts_FinanceContractId",
                        column: x => x.FinanceContractId,
                        principalTable: "Dict_FinanceContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_FinancePayments_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_VehiclePurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SellerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Odometer = table.Column<int>(type: "INTEGER", nullable: false),
                    Payment = table.Column<int>(type: "INTEGER", nullable: false),
                    DownPayment = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    FinanceContractId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_VehiclePurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_VehiclePurchases_Dict_FinanceContracts_FinanceContractId",
                        column: x => x.FinanceContractId,
                        principalTable: "Dict_FinanceContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_VehiclePurchases_Dict_Providers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_VehiclePurchases_Dict_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_VehiclePurchases_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reg_FinanceDebt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_FinanceDebt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_FinanceDebt_Dict_FinanceContracts_Dimensions_ContractId",
                        column: x => x.Dimensions_ContractId,
                        principalTable: "Dict_FinanceContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_FinanceDebt_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_FinanceDebt_Balance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_FinanceDebt_Balance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_FinanceDebt_Balance_Dict_FinanceContracts_Dimensions_ContractId",
                        column: x => x.Dimensions_ContractId,
                        principalTable: "Dict_FinanceContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceStationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Odometer = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    Detailed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    AccidentId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Services_Dict_Providers_ServiceStationId",
                        column: x => x.ServiceStationId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Services_Dict_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Dict_Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Services_Doc_Accidents_AccidentId",
                        column: x => x.AccidentId,
                        principalTable: "Doc_Accidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Services_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_ServiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_ServiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_ServiceLines_Dict_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Dict_Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_ServiceLines_Doc_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Doc_Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_FinanceContracts_Code",
                table: "Dict_FinanceContracts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_FinanceContracts_Import_Source_Import_Key",
                table: "Dict_FinanceContracts",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_FinanceContracts_LenderId",
                table: "Dict_FinanceContracts",
                column: "LenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_FinanceContracts_VehicleId",
                table: "Dict_FinanceContracts",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Items_Code",
                table: "Dict_Items",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Items_Import_Source_Import_Key",
                table: "Dict_Items",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Items_ParentId_Name",
                table: "Dict_Items",
                columns: new[] { "ParentId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Items_Path",
                table: "Dict_Items",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Providers_Code",
                table: "Dict_Providers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Providers_Import_Source_Import_Key",
                table: "Dict_Providers",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Vehicles_Code",
                table: "Dict_Vehicles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Vehicles_Import_Source_Import_Key",
                table: "Dict_Vehicles",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Accidents_VehicleId",
                table: "Doc_Accidents",
                column: "VehicleId");

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
                name: "IX_Doc_FinancePayments_FinanceContractId",
                table: "Doc_FinancePayments",
                column: "FinanceContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Policies_ProviderId",
                table: "Doc_Policies",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Policies_VehicleId",
                table: "Doc_Policies",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Refuels_FuelStationId",
                table: "Doc_Refuels",
                column: "FuelStationId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Refuels_VehicleId",
                table: "Doc_Refuels",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ServiceLines_ItemId",
                table: "Doc_ServiceLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_ServiceLines_ServiceId",
                table: "Doc_ServiceLines",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Services_AccidentId",
                table: "Doc_Services",
                column: "AccidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Services_ServiceStationId",
                table: "Doc_Services",
                column: "ServiceStationId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Services_VehicleId",
                table: "Doc_Services",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_VehiclePurchases_FinanceContractId",
                table: "Doc_VehiclePurchases",
                column: "FinanceContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_VehiclePurchases_SellerId",
                table: "Doc_VehiclePurchases",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_VehiclePurchases_VehicleId",
                table: "Doc_VehiclePurchases",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_VehicleSales_VehicleId",
                table: "Doc_VehicleSales",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_FinanceDebt_Dimensions_ContractId",
                table: "Reg_FinanceDebt",
                column: "Dimensions_ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_FinanceDebt_DocumentId_LineId_IsExpense",
                table: "Reg_FinanceDebt",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_FinanceDebt_Period",
                table: "Reg_FinanceDebt",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_FinanceDebt_Balance_Dimensions_ContractId",
                table: "Reg_FinanceDebt_Balance",
                column: "Dimensions_ContractId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_LastDone_Dimensions_ItemId",
                table: "Reg_LastDone",
                column: "Dimensions_ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_LastDone_Dimensions_VehicleId",
                table: "Reg_LastDone",
                column: "Dimensions_VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_LastDone_DocumentId_LineId",
                table: "Reg_LastDone",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_LastDone_Period",
                table: "Reg_LastDone",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Odometer_Dimensions_VehicleId",
                table: "Reg_Odometer",
                column: "Dimensions_VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Odometer_DocumentId_LineId",
                table: "Reg_Odometer",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Odometer_Period",
                table: "Reg_Odometer",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_PolicyValidity_Dimensions_VehicleId",
                table: "Reg_PolicyValidity",
                column: "Dimensions_VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_PolicyValidity_DocumentId_LineId",
                table: "Reg_PolicyValidity",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_PolicyValidity_Period",
                table: "Reg_PolicyValidity",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ServiceLines_Dimensions_ItemId",
                table: "Reg_ServiceLines",
                column: "Dimensions_ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ServiceLines_Dimensions_VehicleId",
                table: "Reg_ServiceLines",
                column: "Dimensions_VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ServiceLines_DocumentId_LineId_IsExpense",
                table: "Reg_ServiceLines",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_ServiceLines_Period",
                table: "Reg_ServiceLines",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_VehicleCosts_Dimensions_VehicleId",
                table: "Reg_VehicleCosts",
                column: "Dimensions_VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_VehicleCosts_DocumentId_LineId_IsExpense",
                table: "Reg_VehicleCosts",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_VehicleCosts_Period",
                table: "Reg_VehicleCosts",
                column: "Period");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Doc_FinancePayments");

            migrationBuilder.DropTable(
                name: "Doc_Policies");

            migrationBuilder.DropTable(
                name: "Doc_Refuels");

            migrationBuilder.DropTable(
                name: "Doc_ServiceLines");

            migrationBuilder.DropTable(
                name: "Doc_VehiclePurchases");

            migrationBuilder.DropTable(
                name: "Doc_VehicleSales");

            migrationBuilder.DropTable(
                name: "Reg_FinanceDebt");

            migrationBuilder.DropTable(
                name: "Reg_FinanceDebt_Balance");

            migrationBuilder.DropTable(
                name: "Reg_LastDone");

            migrationBuilder.DropTable(
                name: "Reg_Odometer");

            migrationBuilder.DropTable(
                name: "Reg_PolicyValidity");

            migrationBuilder.DropTable(
                name: "Reg_ServiceLines");

            migrationBuilder.DropTable(
                name: "Reg_VehicleCosts");

            migrationBuilder.DropTable(
                name: "Doc_Services");

            migrationBuilder.DropTable(
                name: "Dict_FinanceContracts");

            migrationBuilder.DropTable(
                name: "Dict_Items");

            migrationBuilder.DropTable(
                name: "Doc_Accidents");

            migrationBuilder.DropTable(
                name: "Dict_Providers");

            migrationBuilder.DropTable(
                name: "Dict_Vehicles");

            migrationBuilder.DropTable(
                name: "Doc_Documents");
        }
    }
}
