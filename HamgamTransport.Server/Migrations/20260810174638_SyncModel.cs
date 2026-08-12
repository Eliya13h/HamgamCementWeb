using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamgamTransport.Server.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneralSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersianCompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnglishCompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZmLogoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyLogoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyPhoneNumber1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyPhoneNumber2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyPhoneNumber3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanySite = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalendarType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultTaxPercent = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ParentAccountId = table.Column<int>(type: "int", nullable: true),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    Nature = table.Column<int>(type: "int", nullable: false),
                    IsPostable = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    SystemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountID);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    AttachmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.AttachmentID);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    AttendanceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPresent = table.Column<bool>(type: "bit", nullable: false),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    OvertimeMinutes = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.AttendanceID);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    BankAccountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.BankAccountID);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashBoxes",
                columns: table => new
                {
                    CashBoxID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentCashBoxId = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    IsPettyCash = table.Column<bool>(type: "bit", nullable: false),
                    CeilingAmountInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashBoxes", x => x.CashBoxID);
                    table.ForeignKey(
                        name: "FK_CashBoxes_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashBoxes_CashBoxes_ParentCashBoxId",
                        column: x => x.ParentCashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashBoxUsers",
                columns: table => new
                {
                    CashBoxUserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashBoxUsers", x => x.CashBoxUserID);
                    table.ForeignKey(
                        name: "FK_CashBoxUsers_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashShiftOpeningLines",
                columns: table => new
                {
                    CashShiftOpeningLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashShiftId = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShiftOpeningLines", x => x.CashShiftOpeningLineID);
                });

            migrationBuilder.CreateTable(
                name: "CashShifts",
                columns: table => new
                {
                    CashShiftID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningBalanceInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ClosingTransferAmountInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CashTransferId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShifts", x => x.CashShiftID);
                    table.ForeignKey(
                        name: "FK_CashShifts_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashTransferLines",
                columns: table => new
                {
                    CashTransferLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashTransferId = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashTransferLines", x => x.CashTransferLineID);
                });

            migrationBuilder.CreateTable(
                name: "CashTransfers",
                columns: table => new
                {
                    CashTransferID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromCashBoxId = table.Column<int>(type: "int", nullable: false),
                    ToCashBoxId = table.Column<int>(type: "int", nullable: false),
                    CashShiftId = table.Column<int>(type: "int", nullable: true),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashTransfers", x => x.CashTransferID);
                    table.ForeignKey(
                        name: "FK_CashTransfers_CashBoxes_FromCashBoxId",
                        column: x => x.FromCashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashTransfers_CashBoxes_ToCashBoxId",
                        column: x => x.ToCashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostCenters",
                columns: table => new
                {
                    CostCenterID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenters", x => x.CostCenterID);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    CurrencyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBaseCurrency = table.Column<bool>(type: "bit", nullable: false),
                    DecimalPlaces = table.Column<byte>(type: "tinyint", nullable: false),
                    UseInBothSystems = table.Column<bool>(type: "bit", nullable: false),
                    OriginSystem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.CurrencyID);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyExchangeHistories",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyID = table.Column<int>(type: "int", nullable: false),
                    BaseCurrencyID = table.Column<int>(type: "int", nullable: false),
                    BaseUnitsPerUnit = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    PreviousBaseUnitsPerUnit = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyExchangeHistories", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeHistories_Currencies_BaseCurrencyID",
                        column: x => x.BaseCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeHistories_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyExchangeRates",
                columns: table => new
                {
                    CurrencyExchangeRateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyID = table.Column<int>(type: "int", nullable: false),
                    BaseCurrencyID = table.Column<int>(type: "int", nullable: false),
                    BaseUnitsPerUnit = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceHistoryID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyExchangeRates", x => x.CurrencyExchangeRateID);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeRates_Currencies_BaseCurrencyID",
                        column: x => x.BaseCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeRates_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeRates_CurrencyExchangeHistories_SourceHistoryID",
                        column: x => x.SourceHistoryID,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyExchangeTxns",
                columns: table => new
                {
                    CurrencyExchangeTxnID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExchangeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromCurrencyId = table.Column<int>(type: "int", nullable: false),
                    FromAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FromAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ToCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ToAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ToAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DealRate = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    RecognizeFxDifference = table.Column<bool>(type: "bit", nullable: false),
                    SystemFromBaseUnitsPerUnit = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    SystemToBaseUnitsPerUnit = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    FxDifferenceInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FromCashBoxId = table.Column<int>(type: "int", nullable: true),
                    FromBankAccountId = table.Column<int>(type: "int", nullable: true),
                    ToCashBoxId = table.Column<int>(type: "int", nullable: true),
                    ToBankAccountId = table.Column<int>(type: "int", nullable: true),
                    ExchangeHistoryFromId = table.Column<int>(type: "int", nullable: true),
                    ExchangeHistoryToId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyExchangeTxns", x => x.CurrencyExchangeTxnID);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeTxns_BankAccounts_FromBankAccountId",
                        column: x => x.FromBankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "BankAccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeTxns_BankAccounts_ToBankAccountId",
                        column: x => x.ToBankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "BankAccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeTxns_CashBoxes_FromCashBoxId",
                        column: x => x.FromCashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeTxns_CashBoxes_ToCashBoxId",
                        column: x => x.ToCashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeTxns_Currencies_FromCurrencyId",
                        column: x => x.FromCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeTxns_Currencies_ToCurrencyId",
                        column: x => x.ToCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeTxns_CurrencyExchangeHistories_ExchangeHistoryFromId",
                        column: x => x.ExchangeHistoryFromId,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeTxns_CurrencyExchangeHistories_ExchangeHistoryToId",
                        column: x => x.ExchangeHistoryToId,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CustomerType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerID);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentID);
                });

            migrationBuilder.CreateTable(
                name: "DoubtfulDebtProvisions",
                columns: table => new
                {
                    DoubtfulDebtProvisionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProvisionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubtfulDebtProvisions", x => x.DoubtfulDebtProvisionID);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    DriverId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VehicleOwnerId = table.Column<int>(type: "int", nullable: true),
                    DefaultProfitSharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.DriverId);
                    table.ForeignKey(
                        name: "FK_Drivers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Family = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sallary = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeID);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    ExpenseCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.ExpenseCategoryID);
                    table.ForeignKey(
                        name: "FK_ExpenseCategories_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    ExpenseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    BaseCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeHistoryId = table.Column<int>(type: "int", nullable: true),
                    BaseUnitsPerUnitAtTransaction = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpenseCategoryId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.ExpenseID);
                    table.ForeignKey(
                        name: "FK_Expenses_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_CurrencyExchangeHistories_ExchangeHistoryId",
                        column: x => x.ExchangeHistoryId,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "ExpenseCategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalPeriods",
                columns: table => new
                {
                    FiscalPeriodID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolarYear = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalPeriods", x => x.FiscalPeriodID);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    FiscalYearID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolarYear = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    ClosingJournalEntryId = table.Column<int>(type: "int", nullable: true),
                    EquityAllocationJournalEntryId = table.Column<int>(type: "int", nullable: true),
                    NetIncomeInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.FiscalYearID);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssetCategories",
                columns: table => new
                {
                    FixedAssetCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    AssetAccountId = table.Column<int>(type: "int", nullable: true),
                    AccumulatedDepreciationAccountId = table.Column<int>(type: "int", nullable: true),
                    DepreciationExpenseAccountId = table.Column<int>(type: "int", nullable: true),
                    DefaultUsefulLifeMonths = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssetCategories", x => x.FixedAssetCategoryID);
                    table.ForeignKey(
                        name: "FK_FixedAssetCategories_Accounts_AccumulatedDepreciationAccountId",
                        column: x => x.AccumulatedDepreciationAccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssetCategories_Accounts_AssetAccountId",
                        column: x => x.AssetAccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssetCategories_Accounts_DepreciationExpenseAccountId",
                        column: x => x.DepreciationExpenseAccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssetDepreciations",
                columns: table => new
                {
                    FixedAssetDepreciationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FixedAssetId = table.Column<int>(type: "int", nullable: false),
                    PeriodSolarYear = table.Column<int>(type: "int", nullable: false),
                    PeriodMonth = table.Column<int>(type: "int", nullable: false),
                    DepreciationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssetDepreciations", x => x.FixedAssetDepreciationID);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssets",
                columns: table => new
                {
                    FixedAssetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FixedAssetCategoryId = table.Column<int>(type: "int", nullable: false),
                    AcquisitionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    BaseCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeHistoryId = table.Column<int>(type: "int", nullable: true),
                    BaseUnitsPerUnitAtTransaction = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    CostAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SalvageValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SalvageValueInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UsefulLifeMonths = table.Column<int>(type: "int", nullable: false),
                    DepreciationMethod = table.Column<int>(type: "int", nullable: false),
                    AccumulatedDepreciationInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AcquisitionJournalEntryId = table.Column<int>(type: "int", nullable: true),
                    DisposalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisposalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DisposalAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DisposalJournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssets", x => x.FixedAssetID);
                    table.ForeignKey(
                        name: "FK_FixedAssets_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_CurrencyExchangeHistories_ExchangeHistoryId",
                        column: x => x.ExchangeHistoryId,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FixedAssets_FixedAssetCategories_FixedAssetCategoryId",
                        column: x => x.FixedAssetCategoryId,
                        principalTable: "FixedAssetCategories",
                        principalColumn: "FixedAssetCategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLots",
                columns: table => new
                {
                    InventoryLotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptSequence = table.Column<long>(type: "bigint", nullable: false),
                    ReceivedQuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    RemainingQuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PurchaseInvoiceId = table.Column<int>(type: "int", nullable: true),
                    PurchaseItemId = table.Column<int>(type: "int", nullable: true),
                    ProductionBatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLots", x => x.InventoryLotID);
                });

            migrationBuilder.CreateTable(
                name: "InventoryStocks",
                columns: table => new
                {
                    InventoryStockID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    QuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryStocks", x => x.InventoryStockID);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceInstallments",
                columns: table => new
                {
                    InvoiceInstallmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceKind = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    InstallmentNo = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceInstallments", x => x.InvoiceInstallmentID);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    JournalEntryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    BaseCurrencyId = table.Column<int>(type: "int", nullable: false),
                    TotalDebitInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCreditInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.JournalEntryID);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalLines",
                columns: table => new
                {
                    JournalLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DebitInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreditInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    PartyId = table.Column<int>(type: "int", nullable: true),
                    CostCenterId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalLines", x => x.JournalLineID);
                    table.ForeignKey(
                        name: "FK_JournalLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "CostCenterID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Meaurments",
                columns: table => new
                {
                    MeaurmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsBaseUnit = table.Column<bool>(type: "bit", nullable: false),
                    BaseMeaurmentId = table.Column<int>(type: "int", nullable: true),
                    FactorToBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meaurments", x => x.MeaurmentID);
                    table.ForeignKey(
                        name: "FK_Meaurments_Meaurments_BaseMeaurmentId",
                        column: x => x.BaseMeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerShareAgreements",
                columns: table => new
                {
                    OwnerShareAgreementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehiclePairId = table.Column<int>(type: "int", nullable: false),
                    PrimarySharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    SecondarySharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerShareAgreements", x => x.OwnerShareAgreementId);
                });

            migrationBuilder.CreateTable(
                name: "PartySettlements",
                columns: table => new
                {
                    PartySettlementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartyType = table.Column<int>(type: "int", nullable: false),
                    PartyId = table.Column<int>(type: "int", nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    BankAccountId = table.Column<int>(type: "int", nullable: true),
                    SaleInvoiceId = table.Column<int>(type: "int", nullable: true),
                    PurchaseInvoiceId = table.Column<int>(type: "int", nullable: true),
                    InstallmentId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartySettlements", x => x.PartySettlementID);
                    table.ForeignKey(
                        name: "FK_PartySettlements_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "BankAccountID");
                    table.ForeignKey(
                        name: "FK_PartySettlements_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID");
                    table.ForeignKey(
                        name: "FK_PartySettlements_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartySettlements_InvoiceInstallments_InstallmentId",
                        column: x => x.InstallmentId,
                        principalTable: "InvoiceInstallments",
                        principalColumn: "InvoiceInstallmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID");
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    ProductCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.ProductCategoryID);
                    table.ForeignKey(
                        name: "FK_ProductCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionBatchCostLines",
                columns: table => new
                {
                    ProductionBatchCostLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionBatchId = table.Column<int>(type: "int", nullable: false),
                    CostType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatchCostLines", x => x.ProductionBatchCostLineID);
                    table.ForeignKey(
                        name: "FK_ProductionBatchCostLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionBatches",
                columns: table => new
                {
                    ProductionBatchID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductionFormulaId = table.Column<int>(type: "int", nullable: true),
                    ProductionPlanId = table.Column<int>(type: "int", nullable: true),
                    OutputWarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FixedCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VariableCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalMaterialCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalConversionCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsTransferredToSales = table.Column<bool>(type: "bit", nullable: false),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatches", x => x.ProductionBatchID);
                    table.ForeignKey(
                        name: "FK_ProductionBatches_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCostCategories",
                columns: table => new
                {
                    ProductionCostCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CostType = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCostCategories", x => x.ProductionCostCategoryID);
                    table.ForeignKey(
                        name: "FK_ProductionCostCategories_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCostCategoryDepartments",
                columns: table => new
                {
                    ProductionCostCategoryId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCostCategoryDepartments", x => new { x.ProductionCostCategoryId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_ProductionCostCategoryDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionCostCategoryDepartments_ProductionCostCategories_ProductionCostCategoryId",
                        column: x => x.ProductionCostCategoryId,
                        principalTable: "ProductionCostCategories",
                        principalColumn: "ProductionCostCategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionFormulaCostLines",
                columns: table => new
                {
                    ProductionFormulaCostLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionFormulaId = table.Column<int>(type: "int", nullable: false),
                    CostType = table.Column<int>(type: "int", nullable: false),
                    ProductionCostCategoryId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AmountMode = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionFormulaCostLines", x => x.ProductionFormulaCostLineID);
                    table.ForeignKey(
                        name: "FK_ProductionFormulaCostLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionFormulaCostLines_ProductionCostCategories_ProductionCostCategoryId",
                        column: x => x.ProductionCostCategoryId,
                        principalTable: "ProductionCostCategories",
                        principalColumn: "ProductionCostCategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionFormulaMaterialLines",
                columns: table => new
                {
                    ProductionFormulaMaterialLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionFormulaId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    DefaultWarehouseId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionFormulaMaterialLines", x => x.ProductionFormulaMaterialLineID);
                    table.ForeignKey(
                        name: "FK_ProductionFormulaMaterialLines_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionFormulas",
                columns: table => new
                {
                    ProductionFormulaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionFormulas", x => x.ProductionFormulaID);
                    table.ForeignKey(
                        name: "FK_ProductionFormulas_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionInputLines",
                columns: table => new
                {
                    ProductionInputLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionBatchId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    QuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    MaterialCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionInputLines", x => x.ProductionInputLineID);
                    table.ForeignKey(
                        name: "FK_ProductionInputLines_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionInputLines_ProductionBatches_ProductionBatchId",
                        column: x => x.ProductionBatchId,
                        principalTable: "ProductionBatches",
                        principalColumn: "ProductionBatchID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionInputLotAllocations",
                columns: table => new
                {
                    ProductionInputLotAllocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionInputLineId = table.Column<int>(type: "int", nullable: false),
                    InventoryLotId = table.Column<int>(type: "int", nullable: false),
                    QuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    UnitCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionInputLotAllocations", x => x.ProductionInputLotAllocationID);
                    table.ForeignKey(
                        name: "FK_ProductionInputLotAllocations_InventoryLots_InventoryLotId",
                        column: x => x.InventoryLotId,
                        principalTable: "InventoryLots",
                        principalColumn: "InventoryLotID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionInputLotAllocations_ProductionInputLines_ProductionInputLineId",
                        column: x => x.ProductionInputLineId,
                        principalTable: "ProductionInputLines",
                        principalColumn: "ProductionInputLineID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOutputLines",
                columns: table => new
                {
                    ProductionOutputLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionBatchId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    QuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    UnitCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    InventoryLotId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOutputLines", x => x.ProductionOutputLineID);
                    table.ForeignKey(
                        name: "FK_ProductionOutputLines_InventoryLots_InventoryLotId",
                        column: x => x.InventoryLotId,
                        principalTable: "InventoryLots",
                        principalColumn: "InventoryLotID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOutputLines_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOutputLines_ProductionBatches_ProductionBatchId",
                        column: x => x.ProductionBatchId,
                        principalTable: "ProductionBatches",
                        principalColumn: "ProductionBatchID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPlans",
                columns: table => new
                {
                    ProductionPlanID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPlans", x => x.ProductionPlanID);
                    table.ForeignKey(
                        name: "FK_ProductionPlans_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductMeaurments",
                columns: table => new
                {
                    ProductMeaurmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMeaurments", x => x.ProductMeaurmentID);
                    table.ForeignKey(
                        name: "FK_ProductMeaurments_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BaseMeaurmentId = table.Column<int>(type: "int", nullable: false),
                    DefaultMeaurmentId = table.Column<int>(type: "int", nullable: true),
                    ProductKind = table.Column<int>(type: "int", nullable: false),
                    SalePriceMode = table.Column<int>(type: "int", nullable: false),
                    SaleProfitPercent = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DefaultPurchasePrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DefaultSalePrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MinStockQuantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Products_Meaurments_BaseMeaurmentId",
                        column: x => x.BaseMeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Meaurments_DefaultMeaurmentId",
                        column: x => x.DefaultMeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoices",
                columns: table => new
                {
                    PurchaseInvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    IsCash = table.Column<bool>(type: "bit", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    EntrySource = table.Column<int>(type: "int", nullable: false),
                    ProductionBatchId = table.Column<int>(type: "int", nullable: true),
                    ReferencePurchaseInvoiceId = table.Column<int>(type: "int", nullable: true),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    BaseCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeHistoryId = table.Column<int>(type: "int", nullable: true),
                    BaseUnitsPerUnitAtTransaction = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SubTotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SubTotalAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxPercent = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpenseId = table.Column<int>(type: "int", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseInvoices", x => x.PurchaseInvoiceID);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_CurrencyExchangeHistories_ExchangeHistoryId",
                        column: x => x.ExchangeHistoryId,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "ExpenseID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_ProductionBatches_ProductionBatchId",
                        column: x => x.ProductionBatchId,
                        principalTable: "ProductionBatches",
                        principalColumn: "ProductionBatchID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_PurchaseInvoices_ReferencePurchaseInvoiceId",
                        column: x => x.ReferencePurchaseInvoiceId,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "PurchaseInvoiceID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseItems",
                columns: table => new
                {
                    PurchaseItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    QuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotalInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    InventoryLotId = table.Column<int>(type: "int", nullable: true),
                    ReferencePurchaseItemId = table.Column<int>(type: "int", nullable: true),
                    ReturnedQuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseItems", x => x.PurchaseItemID);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_InventoryLots_InventoryLotId",
                        column: x => x.InventoryLotId,
                        principalTable: "InventoryLots",
                        principalColumn: "InventoryLotID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_PurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "PurchaseInvoiceID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_PurchaseItems_ReferencePurchaseItemId",
                        column: x => x.ReferencePurchaseItemId,
                        principalTable: "PurchaseItems",
                        principalColumn: "PurchaseItemID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringJournalTemplateLines",
                columns: table => new
                {
                    RecurringJournalTemplateLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecurringJournalTemplateId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DebitInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreditInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostCenterId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringJournalTemplateLines", x => x.RecurringJournalTemplateLineID);
                    table.ForeignKey(
                        name: "FK_RecurringJournalTemplateLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringJournalTemplateLines_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "CostCenterID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringJournalTemplates",
                columns: table => new
                {
                    RecurringJournalTemplateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringJournalTemplates", x => x.RecurringJournalTemplateID);
                });

            migrationBuilder.CreateTable(
                name: "RevenueCategories",
                columns: table => new
                {
                    RevenueCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueCategories", x => x.RevenueCategoryID);
                    table.ForeignKey(
                        name: "FK_RevenueCategories_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Revenues",
                columns: table => new
                {
                    RevenueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RevenueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    BaseCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeHistoryId = table.Column<int>(type: "int", nullable: true),
                    BaseUnitsPerUnitAtTransaction = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ProfitInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RevenueCategoryId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revenues", x => x.RevenueID);
                    table.ForeignKey(
                        name: "FK_Revenues_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Revenues_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Revenues_CurrencyExchangeHistories_ExchangeHistoryId",
                        column: x => x.ExchangeHistoryId,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Revenues_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Revenues_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Revenues_RevenueCategories_RevenueCategoryId",
                        column: x => x.RevenueCategoryId,
                        principalTable: "RevenueCategories",
                        principalColumn: "RevenueCategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    HasFullAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Users_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Users_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "SalaryPayments",
                columns: table => new
                {
                    SalaryPaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OvertimeAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LateDeduction = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AbsenceDeduction = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BenefitAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OtherDeduction = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PresentDays = table.Column<int>(type: "int", nullable: false),
                    AbsentDays = table.Column<int>(type: "int", nullable: false),
                    TotalLateMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalOvertimeMinutes = table.Column<int>(type: "int", nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPayments", x => x.SalaryPaymentID);
                    table.ForeignKey(
                        name: "FK_SalaryPayments_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPayments_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalaryPayments_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SalaryPayments_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SalaryPayments_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Shareholders",
                columns: table => new
                {
                    ShareholderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfitShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LossShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shareholders", x => x.ShareholderID);
                    table.ForeignKey(
                        name: "FK_Shareholders_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shareholders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Shareholders_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Shareholders_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SupplierType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierID);
                    table.ForeignKey(
                        name: "FK_Suppliers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Suppliers_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Suppliers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "TripExpenseCategories",
                columns: table => new
                {
                    TripExpenseCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripExpenseCategories", x => x.TripExpenseCategoryId);
                    table.ForeignKey(
                        name: "FK_TripExpenseCategories_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TripExpenseCategories_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TripExpenseCategories_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    UserPermissionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.UserPermissionID);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleOwners",
                columns: table => new
                {
                    VehicleOwnerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleOwners", x => x.VehicleOwnerId);
                    table.ForeignKey(
                        name: "FK_VehicleOwners_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_VehicleOwners_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_VehicleOwners_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_VehicleOwners_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "VehicleTypes",
                columns: table => new
                {
                    VehicleTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultRole = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTypes", x => x.VehicleTypeId);
                    table.ForeignKey(
                        name: "FK_VehicleTypes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_VehicleTypes_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_VehicleTypes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    WarehouseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WarehouseType = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Capacity = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    CapacityMeaurmentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.WarehouseID);
                    table.ForeignKey(
                        name: "FK_Warehouses_Meaurments_CapacityMeaurmentId",
                        column: x => x.CapacityMeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warehouses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Warehouses_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Warehouses_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ShareholderEquityTxns",
                columns: table => new
                {
                    ShareholderEquityTxnID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TxnType = table.Column<int>(type: "int", nullable: false),
                    ShareholderId = table.Column<int>(type: "int", nullable: false),
                    TxnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    BaseCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeHistoryId = table.Column<int>(type: "int", nullable: true),
                    BaseUnitsPerUnitAtTransaction = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ProfitPortionInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CapitalPortionInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    SettlementMode = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareholderEquityTxns", x => x.ShareholderEquityTxnID);
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_CurrencyExchangeHistories_ExchangeHistoryId",
                        column: x => x.ExchangeHistoryId,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID");
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_Shareholders_ShareholderId",
                        column: x => x.ShareholderId,
                        principalTable: "Shareholders",
                        principalColumn: "ShareholderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ShareholderEquityTxns_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "SaleInvoices",
                columns: table => new
                {
                    SaleInvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCash = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    ReferenceSaleInvoiceId = table.Column<int>(type: "int", nullable: true),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    BaseCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeHistoryId = table.Column<int>(type: "int", nullable: true),
                    BaseUnitsPerUnitAtTransaction = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SubTotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SubTotalAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxPercent = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCostInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalProfitInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RevenueId = table.Column<int>(type: "int", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleInvoices", x => x.SaleInvoiceID);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_CurrencyExchangeHistories_ExchangeHistoryId",
                        column: x => x.ExchangeHistoryId,
                        principalTable: "CurrencyExchangeHistories",
                        principalColumn: "HistoryID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Revenues_RevenueId",
                        column: x => x.RevenueId,
                        principalTable: "Revenues",
                        principalColumn: "RevenueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_SaleInvoices_ReferenceSaleInvoiceId",
                        column: x => x.ReferenceSaleInvoiceId,
                        principalTable: "SaleInvoices",
                        principalColumn: "SaleInvoiceID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stocktakings",
                columns: table => new
                {
                    StocktakingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StocktakingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocktakings", x => x.StocktakingID);
                    table.ForeignKey(
                        name: "FK_Stocktakings_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stocktakings_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Stocktakings_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Stocktakings_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Stocktakings_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransfers",
                columns: table => new
                {
                    WarehouseTransferID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromWarehouseId = table.Column<int>(type: "int", nullable: false),
                    ToWarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalCostInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransfers", x => x.WarehouseTransferID);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesItems",
                columns: table => new
                {
                    SalesItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    QuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotalInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineCostInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineProfitInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReferenceSalesItemId = table.Column<int>(type: "int", nullable: true),
                    ReturnedQuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesItems", x => x.SalesItemID);
                    table.ForeignKey(
                        name: "FK_SalesItems_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesItems_SaleInvoices_SaleInvoiceId",
                        column: x => x.SaleInvoiceId,
                        principalTable: "SaleInvoices",
                        principalColumn: "SaleInvoiceID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesItems_SalesItems_ReferenceSalesItemId",
                        column: x => x.ReferenceSalesItemId,
                        principalTable: "SalesItems",
                        principalColumn: "SalesItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesItems_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SalesItems_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SalesItems_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "StocktakingLines",
                columns: table => new
                {
                    StocktakingLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StocktakingId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SystemQuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CountedMeaurmentId = table.Column<int>(type: "int", nullable: false),
                    CountedQuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    DifferenceInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    AdjustmentCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakingLines", x => x.StocktakingLineID);
                    table.ForeignKey(
                        name: "FK_StocktakingLines_Meaurments_CountedMeaurmentId",
                        column: x => x.CountedMeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakingLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakingLines_Stocktakings_StocktakingId",
                        column: x => x.StocktakingId,
                        principalTable: "Stocktakings",
                        principalColumn: "StocktakingID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StocktakingLines_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StocktakingLines_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StocktakingLines_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransferLines",
                columns: table => new
                {
                    WarehouseTransferLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseTransferId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MeaurmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    QuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    UnitCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransferLines", x => x.WarehouseTransferLineID);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_Meaurments_MeaurmentId",
                        column: x => x.MeaurmentId,
                        principalTable: "Meaurments",
                        principalColumn: "MeaurmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_WarehouseTransfers_WarehouseTransferId",
                        column: x => x.WarehouseTransferId,
                        principalTable: "WarehouseTransfers",
                        principalColumn: "WarehouseTransferID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleItemLotAllocations",
                columns: table => new
                {
                    SaleItemLotAllocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesItemId = table.Column<int>(type: "int", nullable: false),
                    InventoryLotId = table.Column<int>(type: "int", nullable: false),
                    PurchaseInvoiceId = table.Column<int>(type: "int", nullable: true),
                    QuantityInBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    UnitCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineCostInBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItemLotAllocations", x => x.SaleItemLotAllocationID);
                    table.ForeignKey(
                        name: "FK_SaleItemLotAllocations_InventoryLots_InventoryLotId",
                        column: x => x.InventoryLotId,
                        principalTable: "InventoryLots",
                        principalColumn: "InventoryLotID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleItemLotAllocations_SalesItems_SalesItemId",
                        column: x => x.SalesItemId,
                        principalTable: "SalesItems",
                        principalColumn: "SalesItemID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleItemLotAllocations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SaleItemLotAllocations_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_SaleItemLotAllocations_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "TransportTrips",
                columns: table => new
                {
                    TransportTripId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TripDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeightTon = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RatePerTon = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VehiclePairId = table.Column<int>(type: "int", nullable: true),
                    PrimaryVehicleId = table.Column<int>(type: "int", nullable: true),
                    SecondaryVehicleId = table.Column<int>(type: "int", nullable: true),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    PrimaryOwnerSharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    SecondaryOwnerSharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    DriverCompensationType = table.Column<int>(type: "int", nullable: false),
                    DriverFixedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DriverProfitSharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevenueJournalEntryId = table.Column<int>(type: "int", nullable: true),
                    IsRevenuePosted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportTrips", x => x.TransportTripId);
                    table.ForeignKey(
                        name: "FK_TransportTrips_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportTrips_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportTrips_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "DriverId");
                    table.ForeignKey(
                        name: "FK_TransportTrips_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TransportTrips_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TransportTrips_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "TripExpenses",
                columns: table => new
                {
                    TripExpenseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransportTripId = table.Column<int>(type: "int", nullable: false),
                    TripExpenseCategoryId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    AmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    BankAccountId = table.Column<int>(type: "int", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripExpenses", x => x.TripExpenseId);
                    table.ForeignKey(
                        name: "FK_TripExpenses_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripExpenses_TransportTrips_TransportTripId",
                        column: x => x.TransportTripId,
                        principalTable: "TransportTrips",
                        principalColumn: "TransportTripId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripExpenses_TripExpenseCategories_TripExpenseCategoryId",
                        column: x => x.TripExpenseCategoryId,
                        principalTable: "TripExpenseCategories",
                        principalColumn: "TripExpenseCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripExpenses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TripExpenses_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_TripExpenses_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "VehiclePairs",
                columns: table => new
                {
                    VehiclePairId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryVehicleId = table.Column<int>(type: "int", nullable: true),
                    SecondaryVehicleId = table.Column<int>(type: "int", nullable: true),
                    PrimarySharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    SecondarySharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiclePairs", x => x.VehiclePairId);
                    table.ForeignKey(
                        name: "FK_VehiclePairs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_VehiclePairs_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_VehiclePairs_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlateNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: false),
                    VehicleOwnerId = table.Column<int>(type: "int", nullable: false),
                    CostCenterId = table.Column<int>(type: "int", nullable: true),
                    VehiclePairId = table.Column<int>(type: "int", nullable: true),
                    RoleInPair = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.VehicleId);
                    table.ForeignKey(
                        name: "FK_Vehicles_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "CostCenterID");
                    table.ForeignKey(
                        name: "FK_Vehicles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Vehicles_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Vehicles_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleOwners_VehicleOwnerId",
                        column: x => x.VehicleOwnerId,
                        principalTable: "VehicleOwners",
                        principalColumn: "VehicleOwnerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehiclePairs_VehiclePairId",
                        column: x => x.VehiclePairId,
                        principalTable: "VehiclePairs",
                        principalColumn: "VehiclePairId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleTypes_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleTypes",
                        principalColumn: "VehicleTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Code",
                table: "Accounts",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CreatedBy",
                table: "Accounts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DeletedBy",
                table: "Accounts",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ParentAccountId",
                table: "Accounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SystemCode",
                table: "Accounts",
                column: "SystemCode",
                unique: true,
                filter: "[IsDeleted] = 0 AND [SystemCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UpdatedBy",
                table: "Accounts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CreatedBy",
                table: "Attachments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_DeletedBy",
                table: "Attachments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_UpdatedBy",
                table: "Attachments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CreatedBy",
                table: "Attendances",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_DeletedBy",
                table: "Attendances",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId_Date",
                table: "Attendances",
                columns: new[] { "EmployeeId", "Date" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_UpdatedBy",
                table: "Attendances",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_AccountId",
                table: "BankAccounts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_CreatedBy",
                table: "BankAccounts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_CurrencyId",
                table: "BankAccounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_DeletedBy",
                table: "BankAccounts",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_UpdatedBy",
                table: "BankAccounts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_AccountId",
                table: "CashBoxes",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_Code",
                table: "CashBoxes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_CreatedBy",
                table: "CashBoxes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_DeletedBy",
                table: "CashBoxes",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_ParentCashBoxId",
                table: "CashBoxes",
                column: "ParentCashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_UpdatedBy",
                table: "CashBoxes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxUsers_CashBoxId_UserId",
                table: "CashBoxUsers",
                columns: new[] { "CashBoxId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxUsers_CreatedBy",
                table: "CashBoxUsers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxUsers_DeletedBy",
                table: "CashBoxUsers",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxUsers_UpdatedBy",
                table: "CashBoxUsers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxUsers_UserId",
                table: "CashBoxUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShiftOpeningLines_CashShiftId",
                table: "CashShiftOpeningLines",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShiftOpeningLines_CreatedBy",
                table: "CashShiftOpeningLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashShiftOpeningLines_CurrencyId",
                table: "CashShiftOpeningLines",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShiftOpeningLines_DeletedBy",
                table: "CashShiftOpeningLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashShiftOpeningLines_UpdatedBy",
                table: "CashShiftOpeningLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_CashBoxId",
                table: "CashShifts",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_CashTransferId",
                table: "CashShifts",
                column: "CashTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_CreatedBy",
                table: "CashShifts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_DeletedBy",
                table: "CashShifts",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_UpdatedBy",
                table: "CashShifts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_UserId",
                table: "CashShifts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransferLines_CashTransferId",
                table: "CashTransferLines",
                column: "CashTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransferLines_CreatedBy",
                table: "CashTransferLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransferLines_CurrencyId",
                table: "CashTransferLines",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransferLines_DeletedBy",
                table: "CashTransferLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransferLines_UpdatedBy",
                table: "CashTransferLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_CreatedBy",
                table: "CashTransfers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_DeletedBy",
                table: "CashTransfers",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_FromCashBoxId",
                table: "CashTransfers",
                column: "FromCashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_JournalEntryId",
                table: "CashTransfers",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_ToCashBoxId",
                table: "CashTransfers",
                column: "ToCashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransfers_UpdatedBy",
                table: "CashTransfers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CreatedBy",
                table: "Categories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DeletedBy",
                table: "Categories",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UpdatedBy",
                table: "Categories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_Code",
                table: "CostCenters",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_CreatedBy",
                table: "CostCenters",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_DeletedBy",
                table: "CostCenters",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_UpdatedBy",
                table: "CostCenters",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_CreatedBy",
                table: "Currencies",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_CurrencyCode",
                table: "Currencies",
                column: "CurrencyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_DeletedBy",
                table: "Currencies",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_UpdatedBy",
                table: "Currencies",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeHistories_BaseCurrencyID",
                table: "CurrencyExchangeHistories",
                column: "BaseCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeHistories_CreatedBy",
                table: "CurrencyExchangeHistories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeHistories_CurrencyID_EffectiveFrom",
                table: "CurrencyExchangeHistories",
                columns: new[] { "CurrencyID", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeHistories_CurrencyID_EffectiveTo",
                table: "CurrencyExchangeHistories",
                columns: new[] { "CurrencyID", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeHistories_DeletedBy",
                table: "CurrencyExchangeHistories",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeHistories_UpdatedBy",
                table: "CurrencyExchangeHistories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeRates_BaseCurrencyID",
                table: "CurrencyExchangeRates",
                column: "BaseCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeRates_CreatedBy",
                table: "CurrencyExchangeRates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeRates_CurrencyID",
                table: "CurrencyExchangeRates",
                column: "CurrencyID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeRates_DeletedBy",
                table: "CurrencyExchangeRates",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeRates_SourceHistoryID",
                table: "CurrencyExchangeRates",
                column: "SourceHistoryID");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeRates_UpdatedBy",
                table: "CurrencyExchangeRates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_CreatedBy",
                table: "CurrencyExchangeTxns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_DeletedBy",
                table: "CurrencyExchangeTxns",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_ExchangeHistoryFromId",
                table: "CurrencyExchangeTxns",
                column: "ExchangeHistoryFromId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_ExchangeHistoryToId",
                table: "CurrencyExchangeTxns",
                column: "ExchangeHistoryToId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_FromBankAccountId",
                table: "CurrencyExchangeTxns",
                column: "FromBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_FromCashBoxId",
                table: "CurrencyExchangeTxns",
                column: "FromCashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_FromCurrencyId",
                table: "CurrencyExchangeTxns",
                column: "FromCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_JournalEntryId",
                table: "CurrencyExchangeTxns",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_ToBankAccountId",
                table: "CurrencyExchangeTxns",
                column: "ToBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_ToCashBoxId",
                table: "CurrencyExchangeTxns",
                column: "ToCashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_ToCurrencyId",
                table: "CurrencyExchangeTxns",
                column: "ToCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeTxns_UpdatedBy",
                table: "CurrencyExchangeTxns",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedBy",
                table: "Customers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DeletedBy",
                table: "Customers",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UpdatedBy",
                table: "Customers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedBy",
                table: "Departments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DeletedBy",
                table: "Departments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_UpdatedBy",
                table: "Departments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DoubtfulDebtProvisions_CreatedBy",
                table: "DoubtfulDebtProvisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DoubtfulDebtProvisions_DeletedBy",
                table: "DoubtfulDebtProvisions",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DoubtfulDebtProvisions_JournalEntryId",
                table: "DoubtfulDebtProvisions",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubtfulDebtProvisions_UpdatedBy",
                table: "DoubtfulDebtProvisions",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_AccountId",
                table: "Drivers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CreatedBy",
                table: "Drivers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_DeletedBy",
                table: "Drivers",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UpdatedBy",
                table: "Drivers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_VehicleOwnerId",
                table: "Drivers",
                column: "VehicleOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CreatedBy",
                table: "Employees",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DeletedBy",
                table: "Employees",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UpdatedBy",
                table: "Employees",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_AccountId",
                table: "ExpenseCategories",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Code",
                table: "ExpenseCategories",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0 AND [Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_CreatedBy",
                table: "ExpenseCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_DeletedBy",
                table: "ExpenseCategories",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_UpdatedBy",
                table: "ExpenseCategories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BaseCurrencyId",
                table: "Expenses",
                column: "BaseCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CreatedBy",
                table: "Expenses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CurrencyId",
                table: "Expenses",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_DeletedBy",
                table: "Expenses",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExchangeHistoryId",
                table: "Expenses",
                column: "ExchangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseCategoryId",
                table: "Expenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_JournalEntryId",
                table: "Expenses",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SupplierId",
                table: "Expenses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UpdatedBy",
                table: "Expenses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_CreatedBy",
                table: "FiscalPeriods",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_DeletedBy",
                table: "FiscalPeriods",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_SolarYear_Month",
                table: "FiscalPeriods",
                columns: new[] { "SolarYear", "Month" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_UpdatedBy",
                table: "FiscalPeriods",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_ClosedByUserId",
                table: "FiscalYears",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_ClosingJournalEntryId",
                table: "FiscalYears",
                column: "ClosingJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_CreatedBy",
                table: "FiscalYears",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_DeletedBy",
                table: "FiscalYears",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_EquityAllocationJournalEntryId",
                table: "FiscalYears",
                column: "EquityAllocationJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_SolarYear",
                table: "FiscalYears",
                column: "SolarYear",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_UpdatedBy",
                table: "FiscalYears",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetCategories_AccumulatedDepreciationAccountId",
                table: "FixedAssetCategories",
                column: "AccumulatedDepreciationAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetCategories_AssetAccountId",
                table: "FixedAssetCategories",
                column: "AssetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetCategories_CreatedBy",
                table: "FixedAssetCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetCategories_DeletedBy",
                table: "FixedAssetCategories",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetCategories_DepreciationExpenseAccountId",
                table: "FixedAssetCategories",
                column: "DepreciationExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetCategories_UpdatedBy",
                table: "FixedAssetCategories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_CreatedBy",
                table: "FixedAssetDepreciations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_DeletedBy",
                table: "FixedAssetDepreciations",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_FixedAssetId_PeriodSolarYear_PeriodMonth",
                table: "FixedAssetDepreciations",
                columns: new[] { "FixedAssetId", "PeriodSolarYear", "PeriodMonth" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_JournalEntryId",
                table: "FixedAssetDepreciations",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_UpdatedBy",
                table: "FixedAssetDepreciations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_AcquisitionJournalEntryId",
                table: "FixedAssets",
                column: "AcquisitionJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_BaseCurrencyId",
                table: "FixedAssets",
                column: "BaseCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_Code",
                table: "FixedAssets",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_CreatedBy",
                table: "FixedAssets",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_CurrencyId",
                table: "FixedAssets",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_DeletedBy",
                table: "FixedAssets",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_DisposalJournalEntryId",
                table: "FixedAssets",
                column: "DisposalJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_ExchangeHistoryId",
                table: "FixedAssets",
                column: "ExchangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_FixedAssetCategoryId",
                table: "FixedAssets",
                column: "FixedAssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_SupplierId",
                table: "FixedAssets",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_UpdatedBy",
                table: "FixedAssets",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_CreatedBy",
                table: "InventoryLots",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_DeletedBy",
                table: "InventoryLots",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_LotCode",
                table: "InventoryLots",
                column: "LotCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_ProductId_WarehouseId_ReceiptSequence",
                table: "InventoryLots",
                columns: new[] { "ProductId", "WarehouseId", "ReceiptSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_UpdatedBy",
                table: "InventoryLots",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_WarehouseId",
                table: "InventoryLots",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_CreatedBy",
                table: "InventoryStocks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_DeletedBy",
                table: "InventoryStocks",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_ProductId",
                table: "InventoryStocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_UpdatedBy",
                table: "InventoryStocks",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_WarehouseId_ProductId",
                table: "InventoryStocks",
                columns: new[] { "WarehouseId", "ProductId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceInstallments_CreatedBy",
                table: "InvoiceInstallments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceInstallments_DeletedBy",
                table: "InvoiceInstallments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceInstallments_UpdatedBy",
                table: "InvoiceInstallments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_BaseCurrencyId",
                table: "JournalEntries",
                column: "BaseCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CreatedBy",
                table: "JournalEntries",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_DeletedBy",
                table: "JournalEntries",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryNumber",
                table: "JournalEntries",
                column: "EntryNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_UpdatedBy",
                table: "JournalEntries",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_AccountId",
                table: "JournalLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_CashBoxId",
                table: "JournalLines",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_CostCenterId",
                table: "JournalLines",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_CreatedBy",
                table: "JournalLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_CurrencyId",
                table: "JournalLines",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_DeletedBy",
                table: "JournalLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_JournalEntryId",
                table: "JournalLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_UpdatedBy",
                table: "JournalLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Meaurments_BaseMeaurmentId_Name",
                table: "Meaurments",
                columns: new[] { "BaseMeaurmentId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsBaseUnit] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Meaurments_CreatedBy",
                table: "Meaurments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Meaurments_DeletedBy",
                table: "Meaurments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Meaurments_Name",
                table: "Meaurments",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsBaseUnit] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Meaurments_UpdatedBy",
                table: "Meaurments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerShareAgreements_CreatedBy",
                table: "OwnerShareAgreements",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerShareAgreements_DeletedBy",
                table: "OwnerShareAgreements",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerShareAgreements_UpdatedBy",
                table: "OwnerShareAgreements",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerShareAgreements_VehiclePairId",
                table: "OwnerShareAgreements",
                column: "VehiclePairId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_BankAccountId",
                table: "PartySettlements",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_CashBoxId",
                table: "PartySettlements",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_CreatedBy",
                table: "PartySettlements",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_CurrencyId",
                table: "PartySettlements",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_DeletedBy",
                table: "PartySettlements",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_InstallmentId",
                table: "PartySettlements",
                column: "InstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_JournalEntryId",
                table: "PartySettlements",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_PurchaseInvoiceId",
                table: "PartySettlements",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_SaleInvoiceId",
                table: "PartySettlements",
                column: "SaleInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_UpdatedBy",
                table: "PartySettlements",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_CategoryId",
                table: "ProductCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_CreatedBy",
                table: "ProductCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_DeletedBy",
                table: "ProductCategories",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ProductId_CategoryId",
                table: "ProductCategories",
                columns: new[] { "ProductId", "CategoryId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_UpdatedBy",
                table: "ProductCategories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchCostLines_AccountId",
                table: "ProductionBatchCostLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchCostLines_CreatedBy",
                table: "ProductionBatchCostLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchCostLines_DeletedBy",
                table: "ProductionBatchCostLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchCostLines_ProductionBatchId",
                table: "ProductionBatchCostLines",
                column: "ProductionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchCostLines_UpdatedBy",
                table: "ProductionBatchCostLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_BatchNumber",
                table: "ProductionBatches",
                column: "BatchNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_CreatedBy",
                table: "ProductionBatches",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_DeletedBy",
                table: "ProductionBatches",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_JournalEntryId",
                table: "ProductionBatches",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_OutputWarehouseId",
                table: "ProductionBatches",
                column: "OutputWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_ProductionFormulaId",
                table: "ProductionBatches",
                column: "ProductionFormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_ProductionPlanId",
                table: "ProductionBatches",
                column: "ProductionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_UpdatedBy",
                table: "ProductionBatches",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCostCategories_AccountId",
                table: "ProductionCostCategories",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCostCategories_Code",
                table: "ProductionCostCategories",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCostCategories_CreatedBy",
                table: "ProductionCostCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCostCategories_DeletedBy",
                table: "ProductionCostCategories",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCostCategories_UpdatedBy",
                table: "ProductionCostCategories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCostCategoryDepartments_DepartmentId",
                table: "ProductionCostCategoryDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaCostLines_AccountId",
                table: "ProductionFormulaCostLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaCostLines_CreatedBy",
                table: "ProductionFormulaCostLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaCostLines_DeletedBy",
                table: "ProductionFormulaCostLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaCostLines_ProductionCostCategoryId",
                table: "ProductionFormulaCostLines",
                column: "ProductionCostCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaCostLines_ProductionFormulaId",
                table: "ProductionFormulaCostLines",
                column: "ProductionFormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaCostLines_UpdatedBy",
                table: "ProductionFormulaCostLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaMaterialLines_CreatedBy",
                table: "ProductionFormulaMaterialLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaMaterialLines_DefaultWarehouseId",
                table: "ProductionFormulaMaterialLines",
                column: "DefaultWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaMaterialLines_DeletedBy",
                table: "ProductionFormulaMaterialLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaMaterialLines_MeaurmentId",
                table: "ProductionFormulaMaterialLines",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaMaterialLines_ProductId",
                table: "ProductionFormulaMaterialLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaMaterialLines_ProductionFormulaId",
                table: "ProductionFormulaMaterialLines",
                column: "ProductionFormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulaMaterialLines_UpdatedBy",
                table: "ProductionFormulaMaterialLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulas_CreatedBy",
                table: "ProductionFormulas",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulas_DeletedBy",
                table: "ProductionFormulas",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulas_MeaurmentId",
                table: "ProductionFormulas",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulas_ProductId",
                table: "ProductionFormulas",
                column: "ProductId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionFormulas_UpdatedBy",
                table: "ProductionFormulas",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLines_CreatedBy",
                table: "ProductionInputLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLines_DeletedBy",
                table: "ProductionInputLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLines_MeaurmentId",
                table: "ProductionInputLines",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLines_ProductId",
                table: "ProductionInputLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLines_ProductionBatchId",
                table: "ProductionInputLines",
                column: "ProductionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLines_UpdatedBy",
                table: "ProductionInputLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLines_WarehouseId",
                table: "ProductionInputLines",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLotAllocations_CreatedBy",
                table: "ProductionInputLotAllocations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLotAllocations_DeletedBy",
                table: "ProductionInputLotAllocations",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLotAllocations_InventoryLotId",
                table: "ProductionInputLotAllocations",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLotAllocations_ProductionInputLineId",
                table: "ProductionInputLotAllocations",
                column: "ProductionInputLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionInputLotAllocations_UpdatedBy",
                table: "ProductionInputLotAllocations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputLines_CreatedBy",
                table: "ProductionOutputLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputLines_DeletedBy",
                table: "ProductionOutputLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputLines_InventoryLotId",
                table: "ProductionOutputLines",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputLines_MeaurmentId",
                table: "ProductionOutputLines",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputLines_ProductId",
                table: "ProductionOutputLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputLines_ProductionBatchId",
                table: "ProductionOutputLines",
                column: "ProductionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputLines_UpdatedBy",
                table: "ProductionOutputLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlans_CreatedBy",
                table: "ProductionPlans",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlans_DeletedBy",
                table: "ProductionPlans",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlans_MeaurmentId",
                table: "ProductionPlans",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlans_ProductId",
                table: "ProductionPlans",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlans_UpdatedBy",
                table: "ProductionPlans",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMeaurments_CreatedBy",
                table: "ProductMeaurments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMeaurments_DeletedBy",
                table: "ProductMeaurments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMeaurments_MeaurmentId",
                table: "ProductMeaurments",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMeaurments_ProductId_MeaurmentId",
                table: "ProductMeaurments",
                columns: new[] { "ProductId", "MeaurmentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMeaurments_UpdatedBy",
                table: "ProductMeaurments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BaseMeaurmentId",
                table: "Products",
                column: "BaseMeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedBy",
                table: "Products",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DefaultMeaurmentId",
                table: "Products",
                column: "DefaultMeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DeletedBy",
                table: "Products",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UpdatedBy",
                table: "Products",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_BaseCurrencyId",
                table: "PurchaseInvoices",
                column: "BaseCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_CashBoxId",
                table: "PurchaseInvoices",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_CreatedBy",
                table: "PurchaseInvoices",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_CurrencyId",
                table: "PurchaseInvoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_DeletedBy",
                table: "PurchaseInvoices",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_ExchangeHistoryId",
                table: "PurchaseInvoices",
                column: "ExchangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_ExpenseId",
                table: "PurchaseInvoices",
                column: "ExpenseId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [ExpenseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_InvoiceNumber",
                table: "PurchaseInvoices",
                column: "InvoiceNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_JournalEntryId",
                table: "PurchaseInvoices",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_ProductionBatchId",
                table: "PurchaseInvoices",
                column: "ProductionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_ReferencePurchaseInvoiceId",
                table: "PurchaseInvoices",
                column: "ReferencePurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_SupplierId",
                table: "PurchaseInvoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_UpdatedBy",
                table: "PurchaseInvoices",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_WarehouseId",
                table: "PurchaseInvoices",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_CreatedBy",
                table: "PurchaseItems",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_DeletedBy",
                table: "PurchaseItems",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_InventoryLotId",
                table: "PurchaseItems",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_MeaurmentId",
                table: "PurchaseItems",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_ProductId",
                table: "PurchaseItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_PurchaseInvoiceId",
                table: "PurchaseItems",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_ReferencePurchaseItemId",
                table: "PurchaseItems",
                column: "ReferencePurchaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_UpdatedBy",
                table: "PurchaseItems",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplateLines_AccountId",
                table: "RecurringJournalTemplateLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplateLines_CostCenterId",
                table: "RecurringJournalTemplateLines",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplateLines_CreatedBy",
                table: "RecurringJournalTemplateLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplateLines_DeletedBy",
                table: "RecurringJournalTemplateLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplateLines_RecurringJournalTemplateId",
                table: "RecurringJournalTemplateLines",
                column: "RecurringJournalTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplateLines_UpdatedBy",
                table: "RecurringJournalTemplateLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplates_CreatedBy",
                table: "RecurringJournalTemplates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplates_DeletedBy",
                table: "RecurringJournalTemplates",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJournalTemplates_UpdatedBy",
                table: "RecurringJournalTemplates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCategories_AccountId",
                table: "RevenueCategories",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCategories_Code",
                table: "RevenueCategories",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0 AND [Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCategories_CreatedBy",
                table: "RevenueCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCategories_DeletedBy",
                table: "RevenueCategories",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCategories_UpdatedBy",
                table: "RevenueCategories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_BaseCurrencyId",
                table: "Revenues",
                column: "BaseCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_CreatedBy",
                table: "Revenues",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_CurrencyId",
                table: "Revenues",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_CustomerId",
                table: "Revenues",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_DeletedBy",
                table: "Revenues",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_ExchangeHistoryId",
                table: "Revenues",
                column: "ExchangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_JournalEntryId",
                table: "Revenues",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_RevenueCategoryId",
                table: "Revenues",
                column: "RevenueCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_UpdatedBy",
                table: "Revenues",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatedBy",
                table: "Roles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DeletedBy",
                table: "Roles",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_UpdatedBy",
                table: "Roles",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_CashBoxId",
                table: "SalaryPayments",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_CreatedBy",
                table: "SalaryPayments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_DeletedBy",
                table: "SalaryPayments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_EmployeeId_Year_Month",
                table: "SalaryPayments",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_JournalEntryId",
                table: "SalaryPayments",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_UpdatedBy",
                table: "SalaryPayments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_BaseCurrencyId",
                table: "SaleInvoices",
                column: "BaseCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_CreatedBy",
                table: "SaleInvoices",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_CurrencyId",
                table: "SaleInvoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_CustomerId",
                table: "SaleInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_DeletedBy",
                table: "SaleInvoices",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_ExchangeHistoryId",
                table: "SaleInvoices",
                column: "ExchangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_InvoiceNumber",
                table: "SaleInvoices",
                column: "InvoiceNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_JournalEntryId",
                table: "SaleInvoices",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_ReferenceSaleInvoiceId",
                table: "SaleInvoices",
                column: "ReferenceSaleInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_RevenueId",
                table: "SaleInvoices",
                column: "RevenueId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [RevenueId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_UpdatedBy",
                table: "SaleInvoices",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_WarehouseId",
                table: "SaleInvoices",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItemLotAllocations_CreatedBy",
                table: "SaleItemLotAllocations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItemLotAllocations_DeletedBy",
                table: "SaleItemLotAllocations",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItemLotAllocations_InventoryLotId",
                table: "SaleItemLotAllocations",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItemLotAllocations_SalesItemId",
                table: "SaleItemLotAllocations",
                column: "SalesItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItemLotAllocations_UpdatedBy",
                table: "SaleItemLotAllocations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_CreatedBy",
                table: "SalesItems",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_DeletedBy",
                table: "SalesItems",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_MeaurmentId",
                table: "SalesItems",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_ProductId",
                table: "SalesItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_ReferenceSalesItemId",
                table: "SalesItems",
                column: "ReferenceSalesItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_SaleInvoiceId",
                table: "SalesItems",
                column: "SaleInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_UpdatedBy",
                table: "SalesItems",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_BaseCurrencyId",
                table: "ShareholderEquityTxns",
                column: "BaseCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_CashBoxId",
                table: "ShareholderEquityTxns",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_CreatedBy",
                table: "ShareholderEquityTxns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_CurrencyId",
                table: "ShareholderEquityTxns",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_DeletedBy",
                table: "ShareholderEquityTxns",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_ExchangeHistoryId",
                table: "ShareholderEquityTxns",
                column: "ExchangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_JournalEntryId",
                table: "ShareholderEquityTxns",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_ShareholderId",
                table: "ShareholderEquityTxns",
                column: "ShareholderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderEquityTxns_UpdatedBy",
                table: "ShareholderEquityTxns",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Shareholders_AccountId",
                table: "Shareholders",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Shareholders_CreatedBy",
                table: "Shareholders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Shareholders_DeletedBy",
                table: "Shareholders",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Shareholders_UpdatedBy",
                table: "Shareholders",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakingLines_CountedMeaurmentId",
                table: "StocktakingLines",
                column: "CountedMeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakingLines_CreatedBy",
                table: "StocktakingLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakingLines_DeletedBy",
                table: "StocktakingLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakingLines_ProductId",
                table: "StocktakingLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakingLines_StocktakingId",
                table: "StocktakingLines",
                column: "StocktakingId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakingLines_UpdatedBy",
                table: "StocktakingLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakings_Code",
                table: "Stocktakings",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakings_CreatedBy",
                table: "Stocktakings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakings_DeletedBy",
                table: "Stocktakings",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakings_JournalEntryId",
                table: "Stocktakings",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakings_UpdatedBy",
                table: "Stocktakings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakings_WarehouseId",
                table: "Stocktakings",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CreatedBy",
                table: "Suppliers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_DeletedBy",
                table: "Suppliers",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_UpdatedBy",
                table: "Suppliers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_CreatedBy",
                table: "TransportTrips",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_CurrencyId",
                table: "TransportTrips",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_CustomerId",
                table: "TransportTrips",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_DeletedBy",
                table: "TransportTrips",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_DriverId",
                table: "TransportTrips",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_PrimaryVehicleId",
                table: "TransportTrips",
                column: "PrimaryVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_SecondaryVehicleId",
                table: "TransportTrips",
                column: "SecondaryVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_TripNumber",
                table: "TransportTrips",
                column: "TripNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_UpdatedBy",
                table: "TransportTrips",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTrips_VehiclePairId",
                table: "TransportTrips",
                column: "VehiclePairId");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenseCategories_CreatedBy",
                table: "TripExpenseCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenseCategories_DeletedBy",
                table: "TripExpenseCategories",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenseCategories_UpdatedBy",
                table: "TripExpenseCategories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenses_CreatedBy",
                table: "TripExpenses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenses_CurrencyId",
                table: "TripExpenses",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenses_DeletedBy",
                table: "TripExpenses",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenses_TransportTripId",
                table: "TripExpenses",
                column: "TransportTripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenses_TripExpenseCategoryId",
                table: "TripExpenses",
                column: "TripExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenses_UpdatedBy",
                table: "TripExpenses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TripExpenses_VehicleId",
                table: "TripExpenses",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId_PermissionKey",
                table: "UserPermissions",
                columns: new[] { "UserId", "PermissionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedBy",
                table: "Users",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedBy",
                table: "Users",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOwners_AccountId",
                table: "VehicleOwners",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOwners_CreatedBy",
                table: "VehicleOwners",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOwners_DeletedBy",
                table: "VehicleOwners",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOwners_UpdatedBy",
                table: "VehicleOwners",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePairs_Code",
                table: "VehiclePairs",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePairs_CreatedBy",
                table: "VehiclePairs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePairs_DeletedBy",
                table: "VehiclePairs",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePairs_PrimaryVehicleId",
                table: "VehiclePairs",
                column: "PrimaryVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePairs_SecondaryVehicleId",
                table: "VehiclePairs",
                column: "SecondaryVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePairs_UpdatedBy",
                table: "VehiclePairs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CostCenterId",
                table: "Vehicles",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CreatedBy",
                table: "Vehicles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_DeletedBy",
                table: "Vehicles",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PlateNumber",
                table: "Vehicles",
                column: "PlateNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UpdatedBy",
                table: "Vehicles",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleOwnerId",
                table: "Vehicles",
                column: "VehicleOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehiclePairId",
                table: "Vehicles",
                column: "VehiclePairId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleTypeId",
                table: "Vehicles",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_Code",
                table: "VehicleTypes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_CreatedBy",
                table: "VehicleTypes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_DeletedBy",
                table: "VehicleTypes",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_UpdatedBy",
                table: "VehicleTypes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CapacityMeaurmentId",
                table: "Warehouses",
                column: "CapacityMeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CreatedBy",
                table: "Warehouses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_DeletedBy",
                table: "Warehouses",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_UpdatedBy",
                table: "Warehouses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_CreatedBy",
                table: "WarehouseTransferLines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_DeletedBy",
                table: "WarehouseTransferLines",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_MeaurmentId",
                table: "WarehouseTransferLines",
                column: "MeaurmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_ProductId",
                table: "WarehouseTransferLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_UpdatedBy",
                table: "WarehouseTransferLines",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_WarehouseTransferId",
                table: "WarehouseTransferLines",
                column: "WarehouseTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_Code",
                table: "WarehouseTransfers",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_CreatedBy",
                table: "WarehouseTransfers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_DeletedBy",
                table: "WarehouseTransfers",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_FromWarehouseId",
                table: "WarehouseTransfers",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_JournalEntryId",
                table: "WarehouseTransfers",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_ToWarehouseId",
                table: "WarehouseTransfers",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_UpdatedBy",
                table: "WarehouseTransfers",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_CreatedBy",
                table: "Accounts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_DeletedBy",
                table: "Accounts",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_UpdatedBy",
                table: "Accounts",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Users_CreatedBy",
                table: "Attachments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Users_DeletedBy",
                table: "Attachments",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Users_UpdatedBy",
                table: "Attachments",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_EmployeeId",
                table: "Attendances",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_CreatedBy",
                table: "Attendances",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_DeletedBy",
                table: "Attendances",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_UpdatedBy",
                table: "Attendances",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_Currencies_CurrencyId",
                table: "BankAccounts",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "CurrencyID");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_Users_CreatedBy",
                table: "BankAccounts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_Users_DeletedBy",
                table: "BankAccounts",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_Users_UpdatedBy",
                table: "BankAccounts",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxes_Users_CreatedBy",
                table: "CashBoxes",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxes_Users_DeletedBy",
                table: "CashBoxes",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxes_Users_UpdatedBy",
                table: "CashBoxes",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxUsers_Users_CreatedBy",
                table: "CashBoxUsers",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxUsers_Users_DeletedBy",
                table: "CashBoxUsers",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxUsers_Users_UpdatedBy",
                table: "CashBoxUsers",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxUsers_Users_UserId",
                table: "CashBoxUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashShiftOpeningLines_CashShifts_CashShiftId",
                table: "CashShiftOpeningLines",
                column: "CashShiftId",
                principalTable: "CashShifts",
                principalColumn: "CashShiftID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CashShiftOpeningLines_Currencies_CurrencyId",
                table: "CashShiftOpeningLines",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "CurrencyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashShiftOpeningLines_Users_CreatedBy",
                table: "CashShiftOpeningLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashShiftOpeningLines_Users_DeletedBy",
                table: "CashShiftOpeningLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashShiftOpeningLines_Users_UpdatedBy",
                table: "CashShiftOpeningLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashShifts_CashTransfers_CashTransferId",
                table: "CashShifts",
                column: "CashTransferId",
                principalTable: "CashTransfers",
                principalColumn: "CashTransferID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashShifts_Users_CreatedBy",
                table: "CashShifts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashShifts_Users_DeletedBy",
                table: "CashShifts",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashShifts_Users_UpdatedBy",
                table: "CashShifts",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashShifts_Users_UserId",
                table: "CashShifts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransferLines_CashTransfers_CashTransferId",
                table: "CashTransferLines",
                column: "CashTransferId",
                principalTable: "CashTransfers",
                principalColumn: "CashTransferID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransferLines_Currencies_CurrencyId",
                table: "CashTransferLines",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "CurrencyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransferLines_Users_CreatedBy",
                table: "CashTransferLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransferLines_Users_DeletedBy",
                table: "CashTransferLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransferLines_Users_UpdatedBy",
                table: "CashTransferLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransfers_JournalEntries_JournalEntryId",
                table: "CashTransfers",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransfers_Users_CreatedBy",
                table: "CashTransfers",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransfers_Users_DeletedBy",
                table: "CashTransfers",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransfers_Users_UpdatedBy",
                table: "CashTransfers",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Users_CreatedBy",
                table: "Categories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Users_DeletedBy",
                table: "Categories",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Users_UpdatedBy",
                table: "Categories",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CostCenters_Users_CreatedBy",
                table: "CostCenters",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CostCenters_Users_DeletedBy",
                table: "CostCenters",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CostCenters_Users_UpdatedBy",
                table: "CostCenters",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Currencies_Users_CreatedBy",
                table: "Currencies",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Currencies_Users_DeletedBy",
                table: "Currencies",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Currencies_Users_UpdatedBy",
                table: "Currencies",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeHistories_Users_CreatedBy",
                table: "CurrencyExchangeHistories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeHistories_Users_DeletedBy",
                table: "CurrencyExchangeHistories",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeHistories_Users_UpdatedBy",
                table: "CurrencyExchangeHistories",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeRates_Users_CreatedBy",
                table: "CurrencyExchangeRates",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeRates_Users_DeletedBy",
                table: "CurrencyExchangeRates",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeRates_Users_UpdatedBy",
                table: "CurrencyExchangeRates",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeTxns_JournalEntries_JournalEntryId",
                table: "CurrencyExchangeTxns",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeTxns_Users_CreatedBy",
                table: "CurrencyExchangeTxns",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeTxns_Users_DeletedBy",
                table: "CurrencyExchangeTxns",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeTxns_Users_UpdatedBy",
                table: "CurrencyExchangeTxns",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_CreatedBy",
                table: "Customers",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_DeletedBy",
                table: "Customers",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_UpdatedBy",
                table: "Customers",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_CreatedBy",
                table: "Departments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_DeletedBy",
                table: "Departments",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_UpdatedBy",
                table: "Departments",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_DoubtfulDebtProvisions_JournalEntries_JournalEntryId",
                table: "DoubtfulDebtProvisions",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoubtfulDebtProvisions_Users_CreatedBy",
                table: "DoubtfulDebtProvisions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_DoubtfulDebtProvisions_Users_DeletedBy",
                table: "DoubtfulDebtProvisions",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_DoubtfulDebtProvisions_Users_UpdatedBy",
                table: "DoubtfulDebtProvisions",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Users_CreatedBy",
                table: "Drivers",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Users_DeletedBy",
                table: "Drivers",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Users_UpdatedBy",
                table: "Drivers",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_VehicleOwners_VehicleOwnerId",
                table: "Drivers",
                column: "VehicleOwnerId",
                principalTable: "VehicleOwners",
                principalColumn: "VehicleOwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Users_CreatedBy",
                table: "Employees",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Users_DeletedBy",
                table: "Employees",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Users_UpdatedBy",
                table: "Employees",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_Users_CreatedBy",
                table: "ExpenseCategories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_Users_DeletedBy",
                table: "ExpenseCategories",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_Users_UpdatedBy",
                table: "ExpenseCategories",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_JournalEntries_JournalEntryId",
                table: "Expenses",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Suppliers_SupplierId",
                table: "Expenses",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_CreatedBy",
                table: "Expenses",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_DeletedBy",
                table: "Expenses",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_UpdatedBy",
                table: "Expenses",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_Users_CreatedBy",
                table: "FiscalPeriods",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_Users_DeletedBy",
                table: "FiscalPeriods",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_Users_UpdatedBy",
                table: "FiscalPeriods",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_JournalEntries_ClosingJournalEntryId",
                table: "FiscalYears",
                column: "ClosingJournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_JournalEntries_EquityAllocationJournalEntryId",
                table: "FiscalYears",
                column: "EquityAllocationJournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_Users_ClosedByUserId",
                table: "FiscalYears",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_Users_CreatedBy",
                table: "FiscalYears",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_Users_DeletedBy",
                table: "FiscalYears",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_Users_UpdatedBy",
                table: "FiscalYears",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssetCategories_Users_CreatedBy",
                table: "FixedAssetCategories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssetCategories_Users_DeletedBy",
                table: "FixedAssetCategories",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssetCategories_Users_UpdatedBy",
                table: "FixedAssetCategories",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssetDepreciations_FixedAssets_FixedAssetId",
                table: "FixedAssetDepreciations",
                column: "FixedAssetId",
                principalTable: "FixedAssets",
                principalColumn: "FixedAssetID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssetDepreciations_JournalEntries_JournalEntryId",
                table: "FixedAssetDepreciations",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssetDepreciations_Users_CreatedBy",
                table: "FixedAssetDepreciations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssetDepreciations_Users_DeletedBy",
                table: "FixedAssetDepreciations",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssetDepreciations_Users_UpdatedBy",
                table: "FixedAssetDepreciations",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssets_JournalEntries_AcquisitionJournalEntryId",
                table: "FixedAssets",
                column: "AcquisitionJournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssets_JournalEntries_DisposalJournalEntryId",
                table: "FixedAssets",
                column: "DisposalJournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssets_Suppliers_SupplierId",
                table: "FixedAssets",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssets_Users_CreatedBy",
                table: "FixedAssets",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssets_Users_DeletedBy",
                table: "FixedAssets",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedAssets_Users_UpdatedBy",
                table: "FixedAssets",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLots_Products_ProductId",
                table: "InventoryLots",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLots_Users_CreatedBy",
                table: "InventoryLots",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLots_Users_DeletedBy",
                table: "InventoryLots",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLots_Users_UpdatedBy",
                table: "InventoryLots",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLots_Warehouses_WarehouseId",
                table: "InventoryLots",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryStocks_Products_ProductId",
                table: "InventoryStocks",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryStocks_Users_CreatedBy",
                table: "InventoryStocks",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryStocks_Users_DeletedBy",
                table: "InventoryStocks",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryStocks_Users_UpdatedBy",
                table: "InventoryStocks",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryStocks_Warehouses_WarehouseId",
                table: "InventoryStocks",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceInstallments_Users_CreatedBy",
                table: "InvoiceInstallments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceInstallments_Users_DeletedBy",
                table: "InvoiceInstallments",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceInstallments_Users_UpdatedBy",
                table: "InvoiceInstallments",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_CreatedBy",
                table: "JournalEntries",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_DeletedBy",
                table: "JournalEntries",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_UpdatedBy",
                table: "JournalEntries",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalLines_Users_CreatedBy",
                table: "JournalLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalLines_Users_DeletedBy",
                table: "JournalLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalLines_Users_UpdatedBy",
                table: "JournalLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Meaurments_Users_CreatedBy",
                table: "Meaurments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Meaurments_Users_DeletedBy",
                table: "Meaurments",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Meaurments_Users_UpdatedBy",
                table: "Meaurments",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_OwnerShareAgreements_Users_CreatedBy",
                table: "OwnerShareAgreements",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_OwnerShareAgreements_Users_DeletedBy",
                table: "OwnerShareAgreements",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_OwnerShareAgreements_Users_UpdatedBy",
                table: "OwnerShareAgreements",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_OwnerShareAgreements_VehiclePairs_VehiclePairId",
                table: "OwnerShareAgreements",
                column: "VehiclePairId",
                principalTable: "VehiclePairs",
                principalColumn: "VehiclePairId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartySettlements_PurchaseInvoices_PurchaseInvoiceId",
                table: "PartySettlements",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "PurchaseInvoiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_PartySettlements_SaleInvoices_SaleInvoiceId",
                table: "PartySettlements",
                column: "SaleInvoiceId",
                principalTable: "SaleInvoices",
                principalColumn: "SaleInvoiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_PartySettlements_Users_CreatedBy",
                table: "PartySettlements",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PartySettlements_Users_DeletedBy",
                table: "PartySettlements",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PartySettlements_Users_UpdatedBy",
                table: "PartySettlements",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Products_ProductId",
                table: "ProductCategories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Users_CreatedBy",
                table: "ProductCategories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Users_DeletedBy",
                table: "ProductCategories",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Users_UpdatedBy",
                table: "ProductCategories",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatchCostLines_ProductionBatches_ProductionBatchId",
                table: "ProductionBatchCostLines",
                column: "ProductionBatchId",
                principalTable: "ProductionBatches",
                principalColumn: "ProductionBatchID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatchCostLines_Users_CreatedBy",
                table: "ProductionBatchCostLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatchCostLines_Users_DeletedBy",
                table: "ProductionBatchCostLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatchCostLines_Users_UpdatedBy",
                table: "ProductionBatchCostLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_ProductionFormulas_ProductionFormulaId",
                table: "ProductionBatches",
                column: "ProductionFormulaId",
                principalTable: "ProductionFormulas",
                principalColumn: "ProductionFormulaID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_ProductionPlans_ProductionPlanId",
                table: "ProductionBatches",
                column: "ProductionPlanId",
                principalTable: "ProductionPlans",
                principalColumn: "ProductionPlanID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Users_CreatedBy",
                table: "ProductionBatches",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Users_DeletedBy",
                table: "ProductionBatches",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Users_UpdatedBy",
                table: "ProductionBatches",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Warehouses_OutputWarehouseId",
                table: "ProductionBatches",
                column: "OutputWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCostCategories_Users_CreatedBy",
                table: "ProductionCostCategories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCostCategories_Users_DeletedBy",
                table: "ProductionCostCategories",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCostCategories_Users_UpdatedBy",
                table: "ProductionCostCategories",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaCostLines_ProductionFormulas_ProductionFormulaId",
                table: "ProductionFormulaCostLines",
                column: "ProductionFormulaId",
                principalTable: "ProductionFormulas",
                principalColumn: "ProductionFormulaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaCostLines_Users_CreatedBy",
                table: "ProductionFormulaCostLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaCostLines_Users_DeletedBy",
                table: "ProductionFormulaCostLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaCostLines_Users_UpdatedBy",
                table: "ProductionFormulaCostLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaMaterialLines_ProductionFormulas_ProductionFormulaId",
                table: "ProductionFormulaMaterialLines",
                column: "ProductionFormulaId",
                principalTable: "ProductionFormulas",
                principalColumn: "ProductionFormulaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaMaterialLines_Products_ProductId",
                table: "ProductionFormulaMaterialLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaMaterialLines_Users_CreatedBy",
                table: "ProductionFormulaMaterialLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaMaterialLines_Users_DeletedBy",
                table: "ProductionFormulaMaterialLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaMaterialLines_Users_UpdatedBy",
                table: "ProductionFormulaMaterialLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulaMaterialLines_Warehouses_DefaultWarehouseId",
                table: "ProductionFormulaMaterialLines",
                column: "DefaultWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulas_Products_ProductId",
                table: "ProductionFormulas",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulas_Users_CreatedBy",
                table: "ProductionFormulas",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulas_Users_DeletedBy",
                table: "ProductionFormulas",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionFormulas_Users_UpdatedBy",
                table: "ProductionFormulas",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionInputLines_Products_ProductId",
                table: "ProductionInputLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionInputLines_Users_CreatedBy",
                table: "ProductionInputLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionInputLines_Users_DeletedBy",
                table: "ProductionInputLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionInputLines_Users_UpdatedBy",
                table: "ProductionInputLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionInputLines_Warehouses_WarehouseId",
                table: "ProductionInputLines",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionInputLotAllocations_Users_CreatedBy",
                table: "ProductionInputLotAllocations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionInputLotAllocations_Users_DeletedBy",
                table: "ProductionInputLotAllocations",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionInputLotAllocations_Users_UpdatedBy",
                table: "ProductionInputLotAllocations",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputLines_Products_ProductId",
                table: "ProductionOutputLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputLines_Users_CreatedBy",
                table: "ProductionOutputLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputLines_Users_DeletedBy",
                table: "ProductionOutputLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputLines_Users_UpdatedBy",
                table: "ProductionOutputLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlans_Products_ProductId",
                table: "ProductionPlans",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlans_Users_CreatedBy",
                table: "ProductionPlans",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlans_Users_DeletedBy",
                table: "ProductionPlans",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlans_Users_UpdatedBy",
                table: "ProductionPlans",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMeaurments_Products_ProductId",
                table: "ProductMeaurments",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMeaurments_Users_CreatedBy",
                table: "ProductMeaurments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMeaurments_Users_DeletedBy",
                table: "ProductMeaurments",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMeaurments_Users_UpdatedBy",
                table: "ProductMeaurments",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Users_CreatedBy",
                table: "Products",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Users_DeletedBy",
                table: "Products",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Users_UpdatedBy",
                table: "Products",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Suppliers_SupplierId",
                table: "PurchaseInvoices",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Users_CreatedBy",
                table: "PurchaseInvoices",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Users_DeletedBy",
                table: "PurchaseInvoices",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Users_UpdatedBy",
                table: "PurchaseInvoices",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Warehouses_WarehouseId",
                table: "PurchaseInvoices",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_Users_CreatedBy",
                table: "PurchaseItems",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_Users_DeletedBy",
                table: "PurchaseItems",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_Users_UpdatedBy",
                table: "PurchaseItems",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringJournalTemplateLines_RecurringJournalTemplates_RecurringJournalTemplateId",
                table: "RecurringJournalTemplateLines",
                column: "RecurringJournalTemplateId",
                principalTable: "RecurringJournalTemplates",
                principalColumn: "RecurringJournalTemplateID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringJournalTemplateLines_Users_CreatedBy",
                table: "RecurringJournalTemplateLines",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringJournalTemplateLines_Users_DeletedBy",
                table: "RecurringJournalTemplateLines",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringJournalTemplateLines_Users_UpdatedBy",
                table: "RecurringJournalTemplateLines",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringJournalTemplates_Users_CreatedBy",
                table: "RecurringJournalTemplates",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringJournalTemplates_Users_DeletedBy",
                table: "RecurringJournalTemplates",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringJournalTemplates_Users_UpdatedBy",
                table: "RecurringJournalTemplates",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RevenueCategories_Users_CreatedBy",
                table: "RevenueCategories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RevenueCategories_Users_DeletedBy",
                table: "RevenueCategories",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RevenueCategories_Users_UpdatedBy",
                table: "RevenueCategories",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Revenues_Users_CreatedBy",
                table: "Revenues",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Revenues_Users_DeletedBy",
                table: "Revenues",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Revenues_Users_UpdatedBy",
                table: "Revenues",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Users_CreatedBy",
                table: "Roles",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Users_DeletedBy",
                table: "Roles",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Users_UpdatedBy",
                table: "Roles",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportTrips_VehiclePairs_VehiclePairId",
                table: "TransportTrips",
                column: "VehiclePairId",
                principalTable: "VehiclePairs",
                principalColumn: "VehiclePairId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportTrips_Vehicles_PrimaryVehicleId",
                table: "TransportTrips",
                column: "PrimaryVehicleId",
                principalTable: "Vehicles",
                principalColumn: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportTrips_Vehicles_SecondaryVehicleId",
                table: "TransportTrips",
                column: "SecondaryVehicleId",
                principalTable: "Vehicles",
                principalColumn: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TripExpenses_Vehicles_VehicleId",
                table: "TripExpenses",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehiclePairs_Vehicles_PrimaryVehicleId",
                table: "VehiclePairs",
                column: "PrimaryVehicleId",
                principalTable: "Vehicles",
                principalColumn: "VehicleId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehiclePairs_Vehicles_SecondaryVehicleId",
                table: "VehiclePairs",
                column: "SecondaryVehicleId",
                principalTable: "Vehicles",
                principalColumn: "VehicleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Users_CreatedBy",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Users_DeletedBy",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Users_UpdatedBy",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_CostCenters_Users_CreatedBy",
                table: "CostCenters");

            migrationBuilder.DropForeignKey(
                name: "FK_CostCenters_Users_DeletedBy",
                table: "CostCenters");

            migrationBuilder.DropForeignKey(
                name: "FK_CostCenters_Users_UpdatedBy",
                table: "CostCenters");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_CreatedBy",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_DeletedBy",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_UpdatedBy",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Users_CreatedBy",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Users_DeletedBy",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Users_UpdatedBy",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Users_CreatedBy",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Users_DeletedBy",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Users_UpdatedBy",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleOwners_Users_CreatedBy",
                table: "VehicleOwners");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleOwners_Users_DeletedBy",
                table: "VehicleOwners");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleOwners_Users_UpdatedBy",
                table: "VehicleOwners");

            migrationBuilder.DropForeignKey(
                name: "FK_VehiclePairs_Users_CreatedBy",
                table: "VehiclePairs");

            migrationBuilder.DropForeignKey(
                name: "FK_VehiclePairs_Users_DeletedBy",
                table: "VehiclePairs");

            migrationBuilder.DropForeignKey(
                name: "FK_VehiclePairs_Users_UpdatedBy",
                table: "VehiclePairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_CreatedBy",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_DeletedBy",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_UpdatedBy",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleTypes_Users_CreatedBy",
                table: "VehicleTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleTypes_Users_DeletedBy",
                table: "VehicleTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleTypes_Users_UpdatedBy",
                table: "VehicleTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleOwners_Accounts_AccountId",
                table: "VehicleOwners");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleOwners_VehicleOwnerId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_CostCenters_CostCenterId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehiclePairs_VehiclePairId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "CashBoxUsers");

            migrationBuilder.DropTable(
                name: "CashShiftOpeningLines");

            migrationBuilder.DropTable(
                name: "CashTransferLines");

            migrationBuilder.DropTable(
                name: "CurrencyExchangeRates");

            migrationBuilder.DropTable(
                name: "CurrencyExchangeTxns");

            migrationBuilder.DropTable(
                name: "DoubtfulDebtProvisions");

            migrationBuilder.DropTable(
                name: "FiscalPeriods");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "FixedAssetDepreciations");

            migrationBuilder.DropTable(
                name: "GeneralSettings");

            migrationBuilder.DropTable(
                name: "InventoryStocks");

            migrationBuilder.DropTable(
                name: "JournalLines");

            migrationBuilder.DropTable(
                name: "OwnerShareAgreements");

            migrationBuilder.DropTable(
                name: "PartySettlements");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "ProductionBatchCostLines");

            migrationBuilder.DropTable(
                name: "ProductionCostCategoryDepartments");

            migrationBuilder.DropTable(
                name: "ProductionFormulaCostLines");

            migrationBuilder.DropTable(
                name: "ProductionFormulaMaterialLines");

            migrationBuilder.DropTable(
                name: "ProductionInputLotAllocations");

            migrationBuilder.DropTable(
                name: "ProductionOutputLines");

            migrationBuilder.DropTable(
                name: "ProductMeaurments");

            migrationBuilder.DropTable(
                name: "PurchaseItems");

            migrationBuilder.DropTable(
                name: "RecurringJournalTemplateLines");

            migrationBuilder.DropTable(
                name: "SalaryPayments");

            migrationBuilder.DropTable(
                name: "SaleItemLotAllocations");

            migrationBuilder.DropTable(
                name: "ShareholderEquityTxns");

            migrationBuilder.DropTable(
                name: "StocktakingLines");

            migrationBuilder.DropTable(
                name: "TripExpenses");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "WarehouseTransferLines");

            migrationBuilder.DropTable(
                name: "CashShifts");

            migrationBuilder.DropTable(
                name: "FixedAssets");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "InvoiceInstallments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "ProductionCostCategories");

            migrationBuilder.DropTable(
                name: "ProductionInputLines");

            migrationBuilder.DropTable(
                name: "PurchaseInvoices");

            migrationBuilder.DropTable(
                name: "RecurringJournalTemplates");

            migrationBuilder.DropTable(
                name: "InventoryLots");

            migrationBuilder.DropTable(
                name: "SalesItems");

            migrationBuilder.DropTable(
                name: "Shareholders");

            migrationBuilder.DropTable(
                name: "Stocktakings");

            migrationBuilder.DropTable(
                name: "TransportTrips");

            migrationBuilder.DropTable(
                name: "TripExpenseCategories");

            migrationBuilder.DropTable(
                name: "WarehouseTransfers");

            migrationBuilder.DropTable(
                name: "CashTransfers");

            migrationBuilder.DropTable(
                name: "FixedAssetCategories");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "ProductionBatches");

            migrationBuilder.DropTable(
                name: "SaleInvoices");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "CashBoxes");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "ProductionFormulas");

            migrationBuilder.DropTable(
                name: "ProductionPlans");

            migrationBuilder.DropTable(
                name: "Revenues");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "CurrencyExchangeHistories");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "RevenueCategories");

            migrationBuilder.DropTable(
                name: "Meaurments");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "VehicleOwners");

            migrationBuilder.DropTable(
                name: "CostCenters");

            migrationBuilder.DropTable(
                name: "VehiclePairs");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "VehicleTypes");
        }
    }
}
