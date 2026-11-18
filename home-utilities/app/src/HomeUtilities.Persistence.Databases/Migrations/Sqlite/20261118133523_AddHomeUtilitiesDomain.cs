using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeUtilities.Persistence.Databases.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddHomeUtilitiesDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dict_Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Area = table.Column<decimal>(type: "TEXT", nullable: false),
                    RegisteredPersonsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Dict_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonalAccountNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Iban = table.Column<string>(type: "TEXT", nullable: true),
                    Edrpou = table.Column<string>(type: "TEXT", nullable: true),
                    BankName = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentDescriptionTemplate = table.Column<string>(type: "TEXT", nullable: true),
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
                name: "Dict_Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsFolder = table.Column<int>(type: "INTEGER", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    Billing = table.Column<int>(type: "INTEGER", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Dict_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Services_Dict_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dict_Services_Dict_Services_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Dict_Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_Settlements_Balance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_Settlements_Balance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_Settlements_Balance_Dict_Properties_Dimensions_PropertyId",
                        column: x => x.Dimensions_PropertyId,
                        principalTable: "Dict_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_Settlements_Balance_Dict_Providers_Dimensions_ProviderId",
                        column: x => x.Dimensions_ProviderId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Month = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentDescription = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Invoices_Dict_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Dict_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Invoices_Dict_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Invoices_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_MeterReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_MeterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_MeterReadings_Dict_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Dict_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_MeterReadings_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_SetTariffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_SetTariffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_SetTariffs_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reg_Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_Settlements_Dict_Properties_Dimensions_PropertyId",
                        column: x => x.Dimensions_PropertyId,
                        principalTable: "Dict_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_Settlements_Dict_Providers_Dimensions_ProviderId",
                        column: x => x.Dimensions_ProviderId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_Settlements_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dict_Meters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Zones = table.Column<int>(type: "INTEGER", nullable: false),
                    NextVerificationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_Dict_Meters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dict_Meters_Dict_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Dict_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dict_Meters_Dict_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Dict_Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_Tariffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_Zone = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_Tariffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_Tariffs_Dict_Services_Dimensions_ServiceId",
                        column: x => x.Dimensions_ServiceId,
                        principalTable: "Dict_Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_Tariffs_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reg_UtilityCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExpense = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dimensions_PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_Zone = table.Column<int>(type: "INTEGER", nullable: false),
                    Resources_Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    Resources_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_UtilityCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_UtilityCosts_Dict_Properties_Dimensions_PropertyId",
                        column: x => x.Dimensions_PropertyId,
                        principalTable: "Dict_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_UtilityCosts_Dict_Services_Dimensions_ServiceId",
                        column: x => x.Dimensions_ServiceId,
                        principalTable: "Dict_Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_UtilityCosts_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentDescription = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Payments_Dict_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Dict_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Payments_Dict_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Dict_Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Payments_Doc_Documents_Id",
                        column: x => x.Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Doc_Payments_Doc_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Doc_Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_SetTariffsLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SetTariffsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Zone = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_SetTariffsLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_SetTariffsLines_Dict_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Dict_Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_SetTariffsLines_Doc_SetTariffs_SetTariffsId",
                        column: x => x.SetTariffsId,
                        principalTable: "Doc_SetTariffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Zone = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_InvoiceLines_Dict_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Dict_Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_InvoiceLines_Dict_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Dict_Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_InvoiceLines_Doc_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Doc_Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_MeterReadingLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeterReadingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Zone = table.Column<int>(type: "INTEGER", nullable: false),
                    Reading = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    PreviousReading = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_MeterReadingLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_MeterReadingLines_Dict_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Dict_Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_MeterReadingLines_Doc_MeterReadings_MeterReadingId",
                        column: x => x.MeterReadingId,
                        principalTable: "Doc_MeterReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reg_MeterReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_MeterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions_Zone = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_Reading = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reg_MeterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reg_MeterReadings_Dict_Meters_Dimensions_MeterId",
                        column: x => x.Dimensions_MeterId,
                        principalTable: "Dict_Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reg_MeterReadings_Doc_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Meters_Code",
                table: "Dict_Meters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Meters_Import_Source_Import_Key",
                table: "Dict_Meters",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Meters_PropertyId",
                table: "Dict_Meters",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Meters_ServiceId",
                table: "Dict_Meters",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Properties_Code",
                table: "Dict_Properties",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Properties_Import_Source_Import_Key",
                table: "Dict_Properties",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

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
                name: "IX_Dict_Services_Code",
                table: "Dict_Services",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Services_Import_Source_Import_Key",
                table: "Dict_Services",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Services_ParentId_Name",
                table: "Dict_Services",
                columns: new[] { "ParentId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Services_Path",
                table: "Dict_Services",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Dict_Services_ProviderId",
                table: "Dict_Services",
                column: "ProviderId");

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
                name: "IX_Doc_InvoiceLines_InvoiceId",
                table: "Doc_InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_InvoiceLines_MeterId",
                table: "Doc_InvoiceLines",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_InvoiceLines_ServiceId",
                table: "Doc_InvoiceLines",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Invoices_PropertyId",
                table: "Doc_Invoices",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Invoices_ProviderId",
                table: "Doc_Invoices",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_MeterReadingLines_MeterId",
                table: "Doc_MeterReadingLines",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_MeterReadingLines_MeterReadingId",
                table: "Doc_MeterReadingLines",
                column: "MeterReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_MeterReadings_PropertyId",
                table: "Doc_MeterReadings",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Payments_InvoiceId",
                table: "Doc_Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Payments_PropertyId",
                table: "Doc_Payments",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Payments_ProviderId",
                table: "Doc_Payments",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_SetTariffsLines_ServiceId",
                table: "Doc_SetTariffsLines",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_SetTariffsLines_SetTariffsId",
                table: "Doc_SetTariffsLines",
                column: "SetTariffsId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_MeterReadings_Dimensions_MeterId",
                table: "Reg_MeterReadings",
                column: "Dimensions_MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_MeterReadings_DocumentId_LineId",
                table: "Reg_MeterReadings",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_MeterReadings_Period",
                table: "Reg_MeterReadings",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Settlements_Dimensions_PropertyId",
                table: "Reg_Settlements",
                column: "Dimensions_PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Settlements_Dimensions_ProviderId",
                table: "Reg_Settlements",
                column: "Dimensions_ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Settlements_DocumentId_LineId_IsExpense",
                table: "Reg_Settlements",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Settlements_Period",
                table: "Reg_Settlements",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Settlements_Balance_Dimensions_PropertyId",
                table: "Reg_Settlements_Balance",
                column: "Dimensions_PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Settlements_Balance_Dimensions_ProviderId_Dimensions_PropertyId",
                table: "Reg_Settlements_Balance",
                columns: new[] { "Dimensions_ProviderId", "Dimensions_PropertyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Tariffs_Dimensions_ServiceId",
                table: "Reg_Tariffs",
                column: "Dimensions_ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Tariffs_DocumentId_LineId",
                table: "Reg_Tariffs",
                columns: new[] { "DocumentId", "LineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reg_Tariffs_Period",
                table: "Reg_Tariffs",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_UtilityCosts_Dimensions_PropertyId",
                table: "Reg_UtilityCosts",
                column: "Dimensions_PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_UtilityCosts_Dimensions_ServiceId",
                table: "Reg_UtilityCosts",
                column: "Dimensions_ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Reg_UtilityCosts_DocumentId_LineId_IsExpense",
                table: "Reg_UtilityCosts",
                columns: new[] { "DocumentId", "LineId", "IsExpense" });

            migrationBuilder.CreateIndex(
                name: "IX_Reg_UtilityCosts_Period",
                table: "Reg_UtilityCosts",
                column: "Period");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Doc_InvoiceLines");

            migrationBuilder.DropTable(
                name: "Doc_MeterReadingLines");

            migrationBuilder.DropTable(
                name: "Doc_Payments");

            migrationBuilder.DropTable(
                name: "Doc_SetTariffsLines");

            migrationBuilder.DropTable(
                name: "Reg_MeterReadings");

            migrationBuilder.DropTable(
                name: "Reg_Settlements");

            migrationBuilder.DropTable(
                name: "Reg_Settlements_Balance");

            migrationBuilder.DropTable(
                name: "Reg_Tariffs");

            migrationBuilder.DropTable(
                name: "Reg_UtilityCosts");

            migrationBuilder.DropTable(
                name: "Doc_MeterReadings");

            migrationBuilder.DropTable(
                name: "Doc_Invoices");

            migrationBuilder.DropTable(
                name: "Doc_SetTariffs");

            migrationBuilder.DropTable(
                name: "Dict_Meters");

            migrationBuilder.DropTable(
                name: "Doc_Documents");

            migrationBuilder.DropTable(
                name: "Dict_Properties");

            migrationBuilder.DropTable(
                name: "Dict_Services");

            migrationBuilder.DropTable(
                name: "Dict_Providers");
        }
    }
}
