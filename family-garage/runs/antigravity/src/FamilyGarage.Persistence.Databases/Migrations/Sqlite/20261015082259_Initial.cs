using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyGarage.Persistence.Databases.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Acc_AccountBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SubcontoHash = table.Column<string>(type: "TEXT", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    DebitTurnover = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    CreditTurnover = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    slot1_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot1_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot2_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot2_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot3_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot3_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot4_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot4_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot5_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    slot5_TypeId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acc_AccountBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Acc_PostingBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acc_PostingBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "App_Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    CanReceiveBroadcasts = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "App_Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastIssuedJwt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastPasswordChange = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLogout = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsBanned = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sched_JobDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessorName = table.Column<string>(type: "TEXT", nullable: true),
                    RunAsUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyOnFailure = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastFailureNotifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Trigger_CronExpression = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Trigger_EndAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Trigger_IntervalSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    Trigger_Misfire = table.Column<int>(type: "INTEGER", nullable: false),
                    Trigger_RepeatCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Trigger_StartAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Trigger_TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Trigger_Type = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Sched_JobDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sched_JobRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    QuartzFireInstanceId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultSummary = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sched_JobRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_Blobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sha256Hash = table.Column<string>(type: "TEXT", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true),
                    StorageKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    InlineContent = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_Blobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_ChangeEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadStorageEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PayloadSchemaVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_ChangeEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConversationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_Constants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValueJson = table.Column<string>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_Sys_Constants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    UserAId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserBId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserALastReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserBLastReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_DocumentLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromEntityTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromLineId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToEntityTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToLineId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EstablishedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_DocumentLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_GaplessCounters",
                columns: table => new
                {
                    Bucket = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CurrentValue = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_GaplessCounters", x => x.Bucket);
                });

            migrationBuilder.CreateTable(
                name: "Sys_GaplessIssued",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Bucket = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    FormattedNumber = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RawValue = table.Column<long>(type: "INTEGER", nullable: true),
                    RequestorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequestorTag = table.Column<string>(type: "TEXT", nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_GaplessIssued", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_Installation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PublicKey = table.Column<byte[]>(type: "BLOB", maxLength: 32, nullable: true),
                    PrivateKey = table.Column<byte[]>(type: "BLOB", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_Installation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceApiKeyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TitleKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BodyKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ArgsJson = table.Column<string>(type: "TEXT", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_SchedulerLease",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HostName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AcquiredUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_SchedulerLease", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_SimpleCounters",
                columns: table => new
                {
                    Bucket = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CurrentValue = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_SimpleCounters", x => x.Bucket);
                });

            migrationBuilder.CreateTable(
                name: "Acc_PostingTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeprecatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SourceAccountCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DestinationAccountCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurrencyAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    dst1_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst1_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst2_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst2_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst3_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst3_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst4_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst4_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst5_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    dst5_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src1_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src1_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src2_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src2_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src3_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src3_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src4_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src4_TypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src5_EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    src5_TypeId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acc_PostingTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Acc_PostingTransactions_Acc_PostingBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Acc_PostingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "App_RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_App_RoleClaims_App_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "App_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "App_RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_App_RefreshTokens_App_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "App_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "App_UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_App_UserClaims_App_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "App_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "App_UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_App_UserLogins_App_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "App_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "App_UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_App_UserRoles_App_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "App_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_App_UserRoles_App_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "App_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "App_UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_App_UserTokens_App_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "App_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sys_ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KeyPrefix = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_ApiKeys_App_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "App_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sys_BlobReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    DeclaredContentType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntityTypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntityTag = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedBySubjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TombstonedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PurgeAfter = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TombstoneReason = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_BlobReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_BlobReferences_Sys_Blobs_BlobId",
                        column: x => x.BlobId,
                        principalTable: "Sys_Blobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sys_NotificationLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityTypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaptionSnapshot = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    NotificationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_NotificationLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_NotificationLinks_Sys_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Sys_Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sys_SimplePools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Bucket = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PoolName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RawValue = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_SimplePools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_SimplePools_Sys_SimpleCounters_Bucket",
                        column: x => x.Bucket,
                        principalTable: "Sys_SimpleCounters",
                        principalColumn: "Bucket",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_AccountBalances_AccountCode_SubcontoHash",
                table: "Acc_AccountBalances",
                columns: new[] { "AccountCode", "SubcontoHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostingBatches_Date",
                table: "Acc_PostingBatches",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostingBatches_DocumentId",
                table: "Acc_PostingBatches",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostingTransactions_BatchId_IsActive",
                table: "Acc_PostingTransactions",
                columns: new[] { "BatchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostingTransactions_DestinationAccountCode_IsActive",
                table: "Acc_PostingTransactions",
                columns: new[] { "DestinationAccountCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostingTransactions_LineId_IsActive",
                table: "Acc_PostingTransactions",
                columns: new[] { "LineId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostingTransactions_SourceAccountCode_IsActive",
                table: "Acc_PostingTransactions",
                columns: new[] { "SourceAccountCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_App_RefreshTokens_UserId",
                table: "App_RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_App_RoleClaims_RoleId",
                table: "App_RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "App_Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_App_UserClaims_UserId",
                table: "App_UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_App_UserLogins_UserId",
                table: "App_UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_App_UserRoles_RoleId",
                table: "App_UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "App_Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "App_Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sched_JobDefinitions_Enabled",
                table: "Sched_JobDefinitions",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_Sched_JobDefinitions_Import_Source_Import_Key",
                table: "Sched_JobDefinitions",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sched_JobDefinitions_Name",
                table: "Sched_JobDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sched_JobRuns_JobDefinitionId_StartedAtUtc",
                table: "Sched_JobRuns",
                columns: new[] { "JobDefinitionId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Sched_JobRuns_QuartzFireInstanceId",
                table: "Sched_JobRuns",
                column: "QuartzFireInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sched_JobRuns_Status",
                table: "Sched_JobRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_ApiKeys_KeyHash",
                table: "Sys_ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sys_ApiKeys_UserId",
                table: "Sys_ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_BlobReferences_BlobId",
                table: "Sys_BlobReferences",
                column: "BlobId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_BlobReferences_EntityTypeId_EntityId",
                table: "Sys_BlobReferences",
                columns: new[] { "EntityTypeId", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_BlobReferences_State_ExpiresAt",
                table: "Sys_BlobReferences",
                columns: new[] { "State", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_BlobReferences_State_PurgeAfter",
                table: "Sys_BlobReferences",
                columns: new[] { "State", "PurgeAfter" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Blobs_Sha256Hash",
                table: "Sys_Blobs",
                column: "Sha256Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sys_ChangeEvents_Date",
                table: "Sys_ChangeEvents",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_ChangeEvents_EntityId_Date",
                table: "Sys_ChangeEvents",
                columns: new[] { "EntityId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_ChangeEvents_RunId",
                table: "Sys_ChangeEvents",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_ChatMessages_ConversationId_SentAt",
                table: "Sys_ChatMessages",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Constants_Import_Source_Import_Key",
                table: "Sys_Constants",
                columns: new[] { "Import_Source", "Import_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Constants_Name_Date",
                table: "Sys_Constants",
                columns: new[] { "Name", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Conversations_Kind_UserAId",
                table: "Sys_Conversations",
                columns: new[] { "Kind", "UserAId" },
                unique: true,
                filter: "\"Kind\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Conversations_Kind_UserAId_UserBId",
                table: "Sys_Conversations",
                columns: new[] { "Kind", "UserAId", "UserBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sys_DocumentLinks_FromEntityId",
                table: "Sys_DocumentLinks",
                column: "FromEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_DocumentLinks_FromEntityId_ToEntityId_ToEntityTypeId_Tag",
                table: "Sys_DocumentLinks",
                columns: new[] { "FromEntityId", "ToEntityId", "ToEntityTypeId", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_DocumentLinks_ToEntityId",
                table: "Sys_DocumentLinks",
                column: "ToEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_GaplessIssued_Bucket_FormattedNumber",
                table: "Sys_GaplessIssued",
                columns: new[] { "Bucket", "FormattedNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sys_NotificationLinks_NotificationId",
                table: "Sys_NotificationLinks",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Notifications_RecipientUserId_DeduplicationKey",
                table: "Sys_Notifications",
                columns: new[] { "RecipientUserId", "DeduplicationKey" },
                unique: true,
                filter: "\"DeduplicationKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Notifications_RecipientUserId_ReadAt",
                table: "Sys_Notifications",
                columns: new[] { "RecipientUserId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_SimplePools_Bucket_PoolName_RawValue",
                table: "Sys_SimplePools",
                columns: new[] { "Bucket", "PoolName", "RawValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_AccBal_slot1_Entity_Acct",
                table: "Acc_AccountBalances",
                columns: new[] { "slot1_EntityId", "AccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_AccBal_slot2_Entity_Acct",
                table: "Acc_AccountBalances",
                columns: new[] { "slot2_EntityId", "AccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_AccBal_slot3_Entity_Acct",
                table: "Acc_AccountBalances",
                columns: new[] { "slot3_EntityId", "AccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_AccBal_slot4_Entity_Acct",
                table: "Acc_AccountBalances",
                columns: new[] { "slot4_EntityId", "AccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_AccBal_slot5_Entity_Acct",
                table: "Acc_AccountBalances",
                columns: new[] { "slot5_EntityId", "AccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_src1_Entity_SrcAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "src1_EntityId", "SourceAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_src2_Entity_SrcAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "src2_EntityId", "SourceAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_src3_Entity_SrcAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "src3_EntityId", "SourceAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_src4_Entity_SrcAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "src4_EntityId", "SourceAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_src5_Entity_SrcAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "src5_EntityId", "SourceAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_dst1_Entity_DstAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "dst1_EntityId", "DestinationAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_dst2_Entity_DstAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "dst2_EntityId", "DestinationAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_dst3_Entity_DstAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "dst3_EntityId", "DestinationAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_dst4_Entity_DstAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "dst4_EntityId", "DestinationAccountCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Acc_PostTx_dst5_Entity_DstAcct",
                table: "Acc_PostingTransactions",
                columns: new[] { "dst5_EntityId", "DestinationAccountCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Acc_AccountBalances");

            migrationBuilder.DropTable(
                name: "Acc_PostingTransactions");

            migrationBuilder.DropTable(
                name: "App_RefreshTokens");

            migrationBuilder.DropTable(
                name: "App_RoleClaims");

            migrationBuilder.DropTable(
                name: "App_UserClaims");

            migrationBuilder.DropTable(
                name: "App_UserLogins");

            migrationBuilder.DropTable(
                name: "App_UserRoles");

            migrationBuilder.DropTable(
                name: "App_UserTokens");

            migrationBuilder.DropTable(
                name: "Sched_JobDefinitions");

            migrationBuilder.DropTable(
                name: "Sched_JobRuns");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Sys_ApiKeys");

            migrationBuilder.DropTable(
                name: "Sys_BlobReferences");

            migrationBuilder.DropTable(
                name: "Sys_ChangeEvents");

            migrationBuilder.DropTable(
                name: "Sys_ChatMessages");

            migrationBuilder.DropTable(
                name: "Sys_Constants");

            migrationBuilder.DropTable(
                name: "Sys_Conversations");

            migrationBuilder.DropTable(
                name: "Sys_DocumentLinks");

            migrationBuilder.DropTable(
                name: "Sys_GaplessCounters");

            migrationBuilder.DropTable(
                name: "Sys_GaplessIssued");

            migrationBuilder.DropTable(
                name: "Sys_Installation");

            migrationBuilder.DropTable(
                name: "Sys_NotificationLinks");

            migrationBuilder.DropTable(
                name: "Sys_SchedulerLease");

            migrationBuilder.DropTable(
                name: "Sys_SimplePools");

            migrationBuilder.DropTable(
                name: "Acc_PostingBatches");

            migrationBuilder.DropTable(
                name: "App_Roles");

            migrationBuilder.DropTable(
                name: "App_Users");

            migrationBuilder.DropTable(
                name: "Sys_Blobs");

            migrationBuilder.DropTable(
                name: "Sys_Notifications");

            migrationBuilder.DropTable(
                name: "Sys_SimpleCounters");
        }
    }
}
