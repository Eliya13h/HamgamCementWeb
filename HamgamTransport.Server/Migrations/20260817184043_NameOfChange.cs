using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamgamTransport.Server.Migrations
{
    /// <inheritdoc />
    public partial class NameOfChange : Migration
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
                    PartyType = table.Column<int>(type: "int", nullable: true),
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
                        principalColumn: "BankAccountID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "JournalEntryID",
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
                    table.PrimaryKey("PK_TripExpenseCategories", x => x.TripExpenseCategoryId);
                    table.ForeignKey(
                        name: "FK_TripExpenseCategories_TripExpenseCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "TripExpenseCategories",
                        principalColumn: "TripExpenseCategoryId",
                        onDelete: ReferentialAction.Restrict);
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
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
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
                    FreightMode = table.Column<int>(type: "int", nullable: false),
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
                    DistributionJournalEntryId = table.Column<int>(type: "int", nullable: true),
                    IsDistributionPosted = table.Column<bool>(type: "bit", nullable: false),
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
                    PartyType = table.Column<int>(type: "int", nullable: true),
                    PartyId = table.Column<int>(type: "int", nullable: true),
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
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: false),
                    VehicleOwnerId = table.Column<int>(type: "int", nullable: false),
                    ChassisNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManufactureYear = table.Column<int>(type: "int", nullable: true),
                    WeightTon = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DefaultIncomeSharePercent = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    DefaultDriverId = table.Column<int>(type: "int", nullable: true),
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
                        name: "FK_Vehicles_Drivers_DefaultDriverId",
                        column: x => x.DefaultDriverId,
                        principalTable: "Drivers",
                        principalColumn: "DriverId");
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
                name: "IX_PartySettlements_JournalEntryId",
                table: "PartySettlements",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_UpdatedBy",
                table: "PartySettlements",
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
                name: "IX_TripExpenseCategories_ParentCategoryId",
                table: "TripExpenseCategories",
                column: "ParentCategoryId");

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
                name: "IX_Vehicles_Code",
                table: "Vehicles",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0 AND [Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CostCenterId",
                table: "Vehicles",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CreatedBy",
                table: "Vehicles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_DefaultDriverId",
                table: "Vehicles",
                column: "DefaultDriverId");

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
                name: "FK_Drivers_Users_CreatedBy",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Users_DeletedBy",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Users_UpdatedBy",
                table: "Drivers");

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
                name: "FK_Drivers_Accounts_AccountId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleOwners_Accounts_AccountId",
                table: "VehicleOwners");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_VehicleOwners_VehicleOwnerId",
                table: "Drivers");

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
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "FiscalPeriods");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "FixedAssetDepreciations");

            migrationBuilder.DropTable(
                name: "GeneralSettings");

            migrationBuilder.DropTable(
                name: "JournalLines");

            migrationBuilder.DropTable(
                name: "OwnerShareAgreements");

            migrationBuilder.DropTable(
                name: "PartySettlements");

            migrationBuilder.DropTable(
                name: "RecurringJournalTemplateLines");

            migrationBuilder.DropTable(
                name: "Revenues");

            migrationBuilder.DropTable(
                name: "ShareholderEquityTxns");

            migrationBuilder.DropTable(
                name: "TripExpenses");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "CashShifts");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "FixedAssets");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "RecurringJournalTemplates");

            migrationBuilder.DropTable(
                name: "RevenueCategories");

            migrationBuilder.DropTable(
                name: "Shareholders");

            migrationBuilder.DropTable(
                name: "TransportTrips");

            migrationBuilder.DropTable(
                name: "TripExpenseCategories");

            migrationBuilder.DropTable(
                name: "CashTransfers");

            migrationBuilder.DropTable(
                name: "CurrencyExchangeHistories");

            migrationBuilder.DropTable(
                name: "FixedAssetCategories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "CashBoxes");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");

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
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "VehicleTypes");
        }
    }
}
