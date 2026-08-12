using Hamgam.Shared.CurrencySync;
using Hamgam.Shared.Data;
using Hamgam.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hamgam.Shared.Services;

public interface ICurrencyReferenceSyncService
{
    Task EnsureReferenceDatabaseAsync(CancellationToken cancellationToken = default);

    Task PushLocalCurrencyToReferenceAsync(string currencyCode, CancellationToken cancellationToken = default);

    Task SyncFromReferenceToLocalAsync(CancellationToken cancellationToken = default);

    Task SeedReferenceFromLocalIfEmptyAsync(CancellationToken cancellationToken = default);
}

public class CurrencyReferenceSyncService : ICurrencyReferenceSyncService
{
    private static readonly SemaphoreSlim SyncGate = new(1, 1);

    private readonly ReferenceDbContext _referenceDb;
    private readonly IConfiguration _configuration;
    private readonly CurrencySyncOptions _options;

    public CurrencyReferenceSyncService(
        ReferenceDbContext referenceDb,
        IConfiguration configuration,
        IOptions<CurrencySyncOptions> options)
    {
        _referenceDb = referenceDb;
        _configuration = configuration;
        _options = options.Value;
    }

    public async Task EnsureReferenceDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var localCs = _configuration.GetConnectionString(_options.LocalConnectionStringName)
                      ?? throw new InvalidOperationException("Local connection string not configured.");
        await CurrencySyncSchemaEnsurer.EnsureLocalCurrencyColumnsAsync(localCs, cancellationToken);
        await _referenceDb.Database.EnsureCreatedAsync(cancellationToken);
    }

    public async Task SeedReferenceFromLocalIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        if (await _referenceDb.Currencies.AnyAsync(cancellationToken))
        {
            return;
        }

        var localCurrencies = await ReadLocalCurrenciesAsync(cancellationToken);
        if (localCurrencies.Count == 0)
        {
            return;
        }

        foreach (var local in localCurrencies)
        {
            await UpsertReferenceFromLocalRowAsync(local, cancellationToken);
        }

        await _referenceDb.SaveChangesAsync(cancellationToken);
    }

    public async Task PushLocalCurrencyToReferenceAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        await SyncGate.WaitAsync(cancellationToken);
        try
        {
            var code = currencyCode.Trim().ToUpperInvariant();
            var local = await ReadLocalCurrencyByCodeAsync(code, cancellationToken);
            if (local is null)
            {
                return;
            }

            await UpsertReferenceFromLocalRowAsync(local, cancellationToken);
            await _referenceDb.SaveChangesAsync(cancellationToken);
            await DeduplicateReferenceHistoriesAsync(cancellationToken);
            await RepairReferenceHistoryTimelineAsync(cancellationToken);
        }
        finally
        {
            SyncGate.Release();
        }
    }

    public async Task SyncFromReferenceToLocalAsync(CancellationToken cancellationToken = default)
    {
        await SyncGate.WaitAsync(cancellationToken);
        try
        {
            var systemCode = _options.SystemCode;
            var referenceCurrencies = await _referenceDb.Currencies
                .AsNoTracking()
                .Where(c => c.IsDeleted != true &&
                            (c.UseInBothSystems || c.OriginSystem == systemCode))
                .ToListAsync(cancellationToken);

            if (referenceCurrencies.Count == 0)
            {
                return;
            }

            var refIds = referenceCurrencies.Select(c => c.CurrencyID).ToList();
            var refRates = await _referenceDb.CurrencyExchangeRates
                .AsNoTracking()
                .Where(r => r.IsDeleted != true && refIds.Contains(r.CurrencyID))
                .ToListAsync(cancellationToken);

            var refHistories = await _referenceDb.CurrencyExchangeHistories
                .AsNoTracking()
                .Where(h => h.IsDeleted != true && refIds.Contains(h.CurrencyID))
                .ToListAsync(cancellationToken);

            // یک ردیف یکتا برای هر نرخ در لحظهٔ مؤثر — ترجیح با ردیفی که EffectiveTo دارد
            var distinctHistories = refHistories
                .GroupBy(h => HistoryKey(h.CurrencyID, h.EffectiveFrom, h.BaseUnitsPerUnit))
                .Select(g => g
                    .OrderByDescending(h => h.EffectiveTo.HasValue)
                    .ThenBy(h => h.HistoryID)
                    .First())
                .ToList();

            await using var connection = CreateLocalConnection();
            await connection.OpenAsync(cancellationToken);

            var localIdByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var appliedHistoryKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var refCurrency in referenceCurrencies)
            {
                var localId = await UpsertLocalCurrencyAsync(connection, refCurrency, cancellationToken);
                localIdByCode[refCurrency.CurrencyCode] = localId;
            }

            foreach (var refRate in refRates)
            {
                var currencyCode = referenceCurrencies.First(c => c.CurrencyID == refRate.CurrencyID).CurrencyCode;
                var baseCode = referenceCurrencies.First(c => c.CurrencyID == refRate.BaseCurrencyID).CurrencyCode;
                if (!localIdByCode.TryGetValue(currencyCode, out var localCurrencyId) ||
                    !localIdByCode.TryGetValue(baseCode, out var localBaseId))
                {
                    continue;
                }

                await UpsertLocalRateAsync(connection, localCurrencyId, localBaseId, refRate, cancellationToken);
            }

            foreach (var refHistory in distinctHistories)
            {
                var currencyCode = referenceCurrencies.First(c => c.CurrencyID == refHistory.CurrencyID).CurrencyCode;
                var baseCode = referenceCurrencies.First(c => c.CurrencyID == refHistory.BaseCurrencyID).CurrencyCode;
                if (!localIdByCode.TryGetValue(currencyCode, out var localCurrencyId) ||
                    !localIdByCode.TryGetValue(baseCode, out var localBaseId))
                {
                    continue;
                }

                var key = HistoryKey(localCurrencyId, refHistory.EffectiveFrom, refHistory.BaseUnitsPerUnit);
                if (!appliedHistoryKeys.Add(key))
                {
                    continue;
                }

                await UpsertLocalHistoryAsync(connection, localCurrencyId, localBaseId, refHistory, cancellationToken);
            }

            await DeduplicateLocalHistoriesAsync(connection, cancellationToken);
            await RepairLocalHistoryTimelineAsync(connection, cancellationToken);
        }
        finally
        {
            SyncGate.Release();
        }
    }

    private async Task UpsertReferenceFromLocalRowAsync(LocalCurrencyRow local, CancellationToken cancellationToken)
    {
        var code = local.CurrencyCode.Trim().ToUpperInvariant();
        var reference = await _referenceDb.Currencies
            .FirstOrDefaultAsync(c => c.CurrencyCode == code, cancellationToken);

        if (reference is null)
        {
            reference = new ReferenceCurrency
            {
                CurrencyCode = code,
                CreatedAt = local.CreatedAt ?? DateTime.Now,
                CreatedBy = local.CreatedBy,
                IsDeleted = false,
                OriginSystem = string.IsNullOrWhiteSpace(local.OriginSystem)
                    ? _options.SystemCode
                    : local.OriginSystem,
            };
            _referenceDb.Currencies.Add(reference);
        }

        reference.Name = local.Name;
        reference.Symbol = local.Symbol;
        reference.Description = local.Description;
        reference.DecimalPlaces = local.DecimalPlaces;
        reference.IsBaseCurrency = local.IsBaseCurrency;
        reference.IsActive = local.IsActive;
        reference.UseInBothSystems = local.UseInBothSystems;
        reference.UpdatedAt = DateTime.Now;
        reference.IsUpdated = true;
        reference.UpdatedBy = local.UpdatedBy;
        reference.IsDeleted = false;

        await _referenceDb.SaveChangesAsync(cancellationToken);

        var localRates = await ReadLocalRatesAsync(local.CurrencyID, cancellationToken);
        foreach (var rate in localRates)
        {
            var localBase = await ReadLocalCurrencyByIdAsync(rate.BaseCurrencyID, cancellationToken);
            if (localBase is null)
            {
                continue;
            }

            var refBase = await _referenceDb.Currencies
                .FirstOrDefaultAsync(c => c.CurrencyCode == localBase.CurrencyCode, cancellationToken);
            if (refBase is null)
            {
                continue;
            }

            var refRate = await _referenceDb.CurrencyExchangeRates
                .FirstOrDefaultAsync(r => r.CurrencyID == reference.CurrencyID, cancellationToken);

            if (refRate is null)
            {
                refRate = new ReferenceCurrencyExchangeRate
                {
                    CurrencyID = reference.CurrencyID,
                    CreatedAt = rate.CreatedAt ?? DateTime.Now,
                    CreatedBy = rate.CreatedBy,
                    IsDeleted = false,
                };
                _referenceDb.CurrencyExchangeRates.Add(refRate);
            }

            refRate.BaseCurrencyID = refBase.CurrencyID;
            refRate.BaseUnitsPerUnit = rate.BaseUnitsPerUnit;
            refRate.EffectiveFrom = NormalizeEffectiveFrom(rate.EffectiveFrom);
            refRate.SourceHistoryID = null;
            refRate.UpdatedAt = DateTime.Now;
            refRate.IsUpdated = true;
            refRate.UpdatedBy = rate.UpdatedBy;
            refRate.IsDeleted = false;
        }

        var localHistories = await ReadLocalHistoriesAsync(local.CurrencyID, cancellationToken);
        var pendingKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var history in localHistories
                     .GroupBy(h => HistoryKey(h.CurrencyID, h.EffectiveFrom, h.BaseUnitsPerUnit))
                     .Select(g => g
                         .OrderByDescending(h => h.EffectiveTo.HasValue)
                         .ThenBy(h => h.HistoryID)
                         .First()))
        {
            var localBase = await ReadLocalCurrencyByIdAsync(history.BaseCurrencyID, cancellationToken);
            if (localBase is null)
            {
                continue;
            }

            var refBase = await _referenceDb.Currencies
                .FirstOrDefaultAsync(c => c.CurrencyCode == localBase.CurrencyCode, cancellationToken);
            if (refBase is null)
            {
                continue;
            }

            var key = HistoryKey(reference.CurrencyID, history.EffectiveFrom, history.BaseUnitsPerUnit);
            if (!pendingKeys.Add(key))
            {
                continue;
            }

            var effectiveFrom = NormalizeEffectiveFrom(history.EffectiveFrom);
            var effectiveTo = history.EffectiveTo.HasValue
                ? NormalizeEffectiveFrom(history.EffectiveTo.Value)
                : (DateTime?)null;

            var existing = await _referenceDb.CurrencyExchangeHistories
                .FirstOrDefaultAsync(
                    h => h.CurrencyID == reference.CurrencyID &&
                         h.IsDeleted != true &&
                         h.BaseUnitsPerUnit == history.BaseUnitsPerUnit &&
                         h.EffectiveFrom >= effectiveFrom &&
                         h.EffectiveFrom < effectiveFrom.AddSeconds(1),
                    cancellationToken);

            if (existing is not null)
            {
                existing.BaseCurrencyID = refBase.CurrencyID;
                existing.PreviousBaseUnitsPerUnit = history.PreviousBaseUnitsPerUnit;
                existing.EffectiveTo = effectiveTo;
                existing.ChangeReason = history.ChangeReason;
                existing.UpdatedAt = DateTime.Now;
                existing.IsUpdated = true;
                existing.IsDeleted = false;
                continue;
            }

            var tracked = _referenceDb.CurrencyExchangeHistories.Local.FirstOrDefault(h =>
                h.CurrencyID == reference.CurrencyID &&
                h.IsDeleted != true &&
                h.BaseUnitsPerUnit == history.BaseUnitsPerUnit &&
                NormalizeEffectiveFrom(h.EffectiveFrom) == effectiveFrom);

            if (tracked is not null)
            {
                tracked.BaseCurrencyID = refBase.CurrencyID;
                tracked.PreviousBaseUnitsPerUnit = history.PreviousBaseUnitsPerUnit;
                tracked.EffectiveTo = effectiveTo;
                tracked.ChangeReason = history.ChangeReason;
                continue;
            }

            _referenceDb.CurrencyExchangeHistories.Add(new ReferenceCurrencyExchangeHistory
            {
                CurrencyID = reference.CurrencyID,
                BaseCurrencyID = refBase.CurrencyID,
                BaseUnitsPerUnit = history.BaseUnitsPerUnit,
                PreviousBaseUnitsPerUnit = history.PreviousBaseUnitsPerUnit,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo,
                ChangeReason = history.ChangeReason,
                CreatedAt = history.CreatedAt ?? DateTime.Now,
                CreatedBy = history.CreatedBy,
                IsDeleted = false,
                IsActive = true,
            });
        }
    }

    private async Task<int> UpsertLocalCurrencyAsync(
        System.Data.Common.DbConnection connection,
        ReferenceCurrency refCurrency,
        CancellationToken cancellationToken)
    {
        // بدون فیلتر IsDeleted — ایندکس یکتا روی CurrencyCode است (مثلاً AFN soft-deleted)
        const string selectSql = """
            SELECT TOP 1 CurrencyID
            FROM Currencies
            WHERE CurrencyCode = @CurrencyCode
            """;

        var existingId = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<int?>(
            connection,
            selectSql,
            new { refCurrency.CurrencyCode });

        if (existingId is int id)
        {
            const string updateSql = """
                UPDATE Currencies SET
                    Name = @Name,
                    Symbol = @Symbol,
                    Description = @Description,
                    DecimalPlaces = @DecimalPlaces,
                    IsBaseCurrency = @IsBaseCurrency,
                    IsActive = @IsActive,
                    UseInBothSystems = @UseInBothSystems,
                    OriginSystem = @OriginSystem,
                    UpdatedAt = @UpdatedAt,
                    IsUpdated = 1,
                    IsDeleted = 0,
                    DeletedAt = NULL,
                    DeletedBy = NULL
                WHERE CurrencyID = @CurrencyID
                """;

            await Dapper.SqlMapper.ExecuteAsync(connection, updateSql, new
            {
                CurrencyID = id,
                refCurrency.Name,
                refCurrency.Symbol,
                refCurrency.Description,
                refCurrency.DecimalPlaces,
                refCurrency.IsBaseCurrency,
                IsActive = refCurrency.IsActive ?? true,
                refCurrency.UseInBothSystems,
                refCurrency.OriginSystem,
                UpdatedAt = DateTime.Now,
            });

            return id;
        }

        try
        {
            const string insertSql = """
                INSERT INTO Currencies
                    (Name, Symbol, CurrencyCode, Description, DecimalPlaces, IsBaseCurrency, IsActive,
                     UseInBothSystems, OriginSystem, CreatedAt, IsDeleted, IsUpdated)
                OUTPUT INSERTED.CurrencyID
                VALUES
                    (@Name, @Symbol, @CurrencyCode, @Description, @DecimalPlaces, @IsBaseCurrency, @IsActive,
                     @UseInBothSystems, @OriginSystem, @CreatedAt, 0, 0)
                """;

            return await Dapper.SqlMapper.QuerySingleAsync<int>(connection, insertSql, new
            {
                refCurrency.Name,
                refCurrency.Symbol,
                refCurrency.CurrencyCode,
                refCurrency.Description,
                refCurrency.DecimalPlaces,
                refCurrency.IsBaseCurrency,
                IsActive = refCurrency.IsActive ?? true,
                refCurrency.UseInBothSystems,
                refCurrency.OriginSystem,
                CreatedAt = DateTime.Now,
            });
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
        {
            // race روی ارز پایه (AFN) — ردیف هم‌زمان درج شده؛ به‌روزرسانی کن
            var racedId = await Dapper.SqlMapper.QuerySingleAsync<int>(
                connection,
                selectSql,
                new { refCurrency.CurrencyCode });

            const string updateSql = """
                UPDATE Currencies SET
                    Name = @Name,
                    Symbol = @Symbol,
                    Description = @Description,
                    DecimalPlaces = @DecimalPlaces,
                    IsBaseCurrency = @IsBaseCurrency,
                    IsActive = @IsActive,
                    UseInBothSystems = @UseInBothSystems,
                    OriginSystem = @OriginSystem,
                    UpdatedAt = @UpdatedAt,
                    IsUpdated = 1,
                    IsDeleted = 0,
                    DeletedAt = NULL,
                    DeletedBy = NULL
                WHERE CurrencyID = @CurrencyID
                """;

            await Dapper.SqlMapper.ExecuteAsync(connection, updateSql, new
            {
                CurrencyID = racedId,
                refCurrency.Name,
                refCurrency.Symbol,
                refCurrency.Description,
                refCurrency.DecimalPlaces,
                refCurrency.IsBaseCurrency,
                IsActive = refCurrency.IsActive ?? true,
                refCurrency.UseInBothSystems,
                refCurrency.OriginSystem,
                UpdatedAt = DateTime.Now,
            });

            return racedId;
        }
    }

    private static async Task UpsertLocalRateAsync(
        System.Data.Common.DbConnection connection,
        int localCurrencyId,
        int localBaseId,
        ReferenceCurrencyExchangeRate refRate,
        CancellationToken cancellationToken)
    {
        const string selectSql = """
            SELECT TOP 1 CurrencyExchangeRateID
            FROM CurrencyExchangeRates
            WHERE CurrencyID = @CurrencyID AND (IsDeleted = 0 OR IsDeleted IS NULL)
            """;

        var existingId = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<int?>(
            connection,
            selectSql,
            new { CurrencyID = localCurrencyId });

        var effectiveFrom = NormalizeEffectiveFrom(refRate.EffectiveFrom);

        if (existingId is int id)
        {
            const string updateSql = """
                UPDATE CurrencyExchangeRates SET
                    BaseCurrencyID = @BaseCurrencyID,
                    BaseUnitsPerUnit = @BaseUnitsPerUnit,
                    EffectiveFrom = @EffectiveFrom,
                    UpdatedAt = @UpdatedAt,
                    IsUpdated = 1,
                    IsDeleted = 0
                WHERE CurrencyExchangeRateID = @CurrencyExchangeRateID
                """;

            await Dapper.SqlMapper.ExecuteAsync(connection, updateSql, new
            {
                CurrencyExchangeRateID = id,
                BaseCurrencyID = localBaseId,
                refRate.BaseUnitsPerUnit,
                EffectiveFrom = effectiveFrom,
                UpdatedAt = DateTime.Now,
            });
            return;
        }

        const string insertSql = """
            INSERT INTO CurrencyExchangeRates
                (CurrencyID, BaseCurrencyID, BaseUnitsPerUnit, EffectiveFrom, CreatedAt, IsDeleted, IsActive, IsUpdated)
            VALUES
                (@CurrencyID, @BaseCurrencyID, @BaseUnitsPerUnit, @EffectiveFrom, @CreatedAt, 0, 1, 0)
            """;

        await Dapper.SqlMapper.ExecuteAsync(connection, insertSql, new
        {
            CurrencyID = localCurrencyId,
            BaseCurrencyID = localBaseId,
            refRate.BaseUnitsPerUnit,
            EffectiveFrom = effectiveFrom,
            CreatedAt = DateTime.Now,
        });
    }

    private static async Task UpsertLocalHistoryAsync(
        System.Data.Common.DbConnection connection,
        int localCurrencyId,
        int localBaseId,
        ReferenceCurrencyExchangeHistory refHistory,
        CancellationToken cancellationToken)
    {
        var effectiveFrom = NormalizeEffectiveFrom(refHistory.EffectiveFrom);
        var effectiveTo = refHistory.EffectiveTo.HasValue
            ? NormalizeEffectiveFrom(refHistory.EffectiveTo.Value)
            : (DateTime?)null;

        // مقایسه بازه‌ای به‌جای برابری دقیق — Dapper گاهی datetime را با دقت کمتر می‌فرستد و با datetime2 نمی‌خواند
        const string existsSql = """
            SELECT TOP 1 HistoryID
            FROM CurrencyExchangeHistories
            WHERE CurrencyID = @CurrencyID
              AND BaseUnitsPerUnit = @BaseUnitsPerUnit
              AND EffectiveFrom >= @EffectiveFrom
              AND EffectiveFrom < DATEADD(SECOND, 1, @EffectiveFrom)
              AND (IsDeleted = 0 OR IsDeleted IS NULL)
            """;

        var exists = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<int?>(
            connection,
            existsSql,
            new
            {
                CurrencyID = localCurrencyId,
                BaseUnitsPerUnit = refHistory.BaseUnitsPerUnit,
                EffectiveFrom = effectiveFrom,
            });

        if (exists is int historyId)
        {
            // مهم: پایان دوره (EffectiveTo) باید از مرجع همگام شود؛ وگرنه همه ردیف‌ها «جاری» می‌مانند
            const string updateSql = """
                UPDATE CurrencyExchangeHistories SET
                    BaseCurrencyID = @BaseCurrencyID,
                    PreviousBaseUnitsPerUnit = @PreviousBaseUnitsPerUnit,
                    EffectiveTo = @EffectiveTo,
                    ChangeReason = @ChangeReason,
                    UpdatedAt = @UpdatedAt,
                    IsUpdated = 1,
                    IsDeleted = 0
                WHERE HistoryID = @HistoryID
                """;

            await Dapper.SqlMapper.ExecuteAsync(connection, updateSql, new
            {
                HistoryID = historyId,
                BaseCurrencyID = localBaseId,
                refHistory.PreviousBaseUnitsPerUnit,
                EffectiveTo = effectiveTo,
                refHistory.ChangeReason,
                UpdatedAt = DateTime.Now,
            });
            return;
        }

        const string insertSql = """
            INSERT INTO CurrencyExchangeHistories
                (CurrencyID, BaseCurrencyID, BaseUnitsPerUnit, PreviousBaseUnitsPerUnit,
                 EffectiveFrom, EffectiveTo, ChangeReason, CreatedAt, IsDeleted, IsActive, IsUpdated)
            VALUES
                (@CurrencyID, @BaseCurrencyID, @BaseUnitsPerUnit, @PreviousBaseUnitsPerUnit,
                 @EffectiveFrom, @EffectiveTo, @ChangeReason, @CreatedAt, 0, 1, 0)
            """;

        await Dapper.SqlMapper.ExecuteAsync(connection, insertSql, new
        {
            CurrencyID = localCurrencyId,
            BaseCurrencyID = localBaseId,
            refHistory.BaseUnitsPerUnit,
            refHistory.PreviousBaseUnitsPerUnit,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            refHistory.ChangeReason,
            CreatedAt = DateTime.Now,
        });
    }

    private async Task DeduplicateReferenceHistoriesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            ;WITH ranked AS (
                SELECT HistoryID,
                       ROW_NUMBER() OVER (
                           PARTITION BY CurrencyID, BaseUnitsPerUnit,
                                        CONVERT(datetime2(0), EffectiveFrom)
                           ORDER BY HistoryID
                       ) AS rn
                FROM CurrencyExchangeHistories
                WHERE IsDeleted = 0 OR IsDeleted IS NULL
            )
            UPDATE h
            SET IsDeleted = 1,
                DeletedAt = SYSUTCDATETIME(),
                IsUpdated = 1
            FROM CurrencyExchangeHistories h
            INNER JOIN ranked r ON r.HistoryID = h.HistoryID
            WHERE r.rn > 1
            """;

        await _referenceDb.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static async Task DeduplicateLocalHistoriesAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            ;WITH ranked AS (
                SELECT HistoryID,
                       ROW_NUMBER() OVER (
                           PARTITION BY CurrencyID, BaseUnitsPerUnit,
                                        CONVERT(datetime2(0), EffectiveFrom)
                           ORDER BY HistoryID
                       ) AS rn
                FROM CurrencyExchangeHistories
                WHERE IsDeleted = 0 OR IsDeleted IS NULL
            )
            UPDATE h
            SET IsDeleted = 1,
                DeletedAt = SYSUTCDATETIME(),
                IsUpdated = 1
            FROM CurrencyExchangeHistories h
            INNER JOIN ranked r ON r.HistoryID = h.HistoryID
            WHERE r.rn > 1
            """;

        await Dapper.SqlMapper.ExecuteAsync(connection, new Dapper.CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// هر دوره به‌جز آخرین نرخ باید EffectiveTo = شروع دوره بعدی داشته باشد.
    /// </summary>
    private static async Task RepairLocalHistoryTimelineAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            ;WITH ordered AS (
                SELECT HistoryID,
                       LEAD(EffectiveFrom) OVER (
                           PARTITION BY CurrencyID
                           ORDER BY EffectiveFrom, HistoryID
                       ) AS NextFrom
                FROM CurrencyExchangeHistories
                WHERE IsDeleted = 0 OR IsDeleted IS NULL
            )
            UPDATE h
            SET EffectiveTo = o.NextFrom,
                UpdatedAt = SYSUTCDATETIME(),
                IsUpdated = 1
            FROM CurrencyExchangeHistories h
            INNER JOIN ordered o ON o.HistoryID = h.HistoryID
            WHERE ISNULL(CONVERT(datetime2(0), h.EffectiveTo), CONVERT(datetime2(0), '9999-12-31'))
                <> ISNULL(CONVERT(datetime2(0), o.NextFrom), CONVERT(datetime2(0), '9999-12-31'))
            """;

        await Dapper.SqlMapper.ExecuteAsync(connection, new Dapper.CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private async Task RepairReferenceHistoryTimelineAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            ;WITH ordered AS (
                SELECT HistoryID,
                       LEAD(EffectiveFrom) OVER (
                           PARTITION BY CurrencyID
                           ORDER BY EffectiveFrom, HistoryID
                       ) AS NextFrom
                FROM CurrencyExchangeHistories
                WHERE IsDeleted = 0 OR IsDeleted IS NULL
            )
            UPDATE h
            SET EffectiveTo = o.NextFrom,
                UpdatedAt = SYSUTCDATETIME(),
                IsUpdated = 1
            FROM CurrencyExchangeHistories h
            INNER JOIN ordered o ON o.HistoryID = h.HistoryID
            WHERE ISNULL(CONVERT(datetime2(0), h.EffectiveTo), CONVERT(datetime2(0), '9999-12-31'))
                <> ISNULL(CONVERT(datetime2(0), o.NextFrom), CONVERT(datetime2(0), '9999-12-31'))
            """;

        await _referenceDb.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static DateTime NormalizeEffectiveFrom(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        return new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, local.Second, DateTimeKind.Unspecified);
    }

    private static string HistoryKey(int currencyId, DateTime effectiveFrom, decimal rate)
    {
        var normalized = NormalizeEffectiveFrom(effectiveFrom);
        return $"{currencyId}|{normalized:yyyy-MM-dd HH:mm:ss}|{rate:0.########}";
    }

    private Microsoft.Data.SqlClient.SqlConnection CreateLocalConnection()
    {
        var cs = _configuration.GetConnectionString(_options.LocalConnectionStringName)
                 ?? throw new InvalidOperationException("Local connection string not configured.");
        return new Microsoft.Data.SqlClient.SqlConnection(cs);
    }

    private async Task<List<LocalCurrencyRow>> ReadLocalCurrenciesAsync(CancellationToken cancellationToken)
    {
        await using var connection = CreateLocalConnection();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT CurrencyID, Name, Symbol, CurrencyCode, Description, DecimalPlaces, IsBaseCurrency,
                   IsActive, UseInBothSystems, OriginSystem, CreatedAt, CreatedBy, UpdatedBy
            FROM Currencies
            WHERE IsDeleted = 0 OR IsDeleted IS NULL
            """;
        var rows = await Dapper.SqlMapper.QueryAsync<LocalCurrencyRow>(connection, sql);
        return rows.ToList();
    }

    private async Task<LocalCurrencyRow?> ReadLocalCurrencyByCodeAsync(string code, CancellationToken cancellationToken)
    {
        await using var connection = CreateLocalConnection();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT CurrencyID, Name, Symbol, CurrencyCode, Description, DecimalPlaces, IsBaseCurrency,
                   IsActive, UseInBothSystems, OriginSystem, CreatedAt, CreatedBy, UpdatedBy
            FROM Currencies
            WHERE CurrencyCode = @Code AND (IsDeleted = 0 OR IsDeleted IS NULL)
            """;
        return await Dapper.SqlMapper.QueryFirstOrDefaultAsync<LocalCurrencyRow>(
            connection,
            sql,
            new { Code = code });
    }

    private async Task<LocalCurrencyRow?> ReadLocalCurrencyByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = CreateLocalConnection();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT CurrencyID, Name, Symbol, CurrencyCode, Description, DecimalPlaces, IsBaseCurrency,
                   IsActive, UseInBothSystems, OriginSystem, CreatedAt, CreatedBy, UpdatedBy
            FROM Currencies
            WHERE CurrencyID = @Id
            """;
        return await Dapper.SqlMapper.QueryFirstOrDefaultAsync<LocalCurrencyRow>(
            connection,
            sql,
            new { Id = id });
    }

    private async Task<List<LocalRateRow>> ReadLocalRatesAsync(int currencyId, CancellationToken cancellationToken)
    {
        await using var connection = CreateLocalConnection();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT CurrencyExchangeRateID, CurrencyID, BaseCurrencyID, BaseUnitsPerUnit, EffectiveFrom,
                   CreatedAt, CreatedBy, UpdatedBy
            FROM CurrencyExchangeRates
            WHERE CurrencyID = @CurrencyID AND (IsDeleted = 0 OR IsDeleted IS NULL)
            """;
        var rows = await Dapper.SqlMapper.QueryAsync<LocalRateRow>(connection, sql, new { CurrencyID = currencyId });
        return rows.ToList();
    }

    private async Task<List<LocalHistoryRow>> ReadLocalHistoriesAsync(int currencyId, CancellationToken cancellationToken)
    {
        await using var connection = CreateLocalConnection();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT HistoryID, CurrencyID, BaseCurrencyID, BaseUnitsPerUnit, PreviousBaseUnitsPerUnit,
                   EffectiveFrom, EffectiveTo, ChangeReason, CreatedAt, CreatedBy
            FROM CurrencyExchangeHistories
            WHERE CurrencyID = @CurrencyID AND (IsDeleted = 0 OR IsDeleted IS NULL)
            """;
        var rows = await Dapper.SqlMapper.QueryAsync<LocalHistoryRow>(connection, sql, new { CurrencyID = currencyId });
        return rows.ToList();
    }

    private sealed class LocalCurrencyRow
    {
        public int CurrencyID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public byte DecimalPlaces { get; set; }
        public bool IsBaseCurrency { get; set; }
        public bool? IsActive { get; set; }
        public bool UseInBothSystems { get; set; }
        public string OriginSystem { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }

    private sealed class LocalRateRow
    {
        public int CurrencyExchangeRateID { get; set; }
        public int CurrencyID { get; set; }
        public int BaseCurrencyID { get; set; }
        public decimal BaseUnitsPerUnit { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }

    private sealed class LocalHistoryRow
    {
        public int HistoryID { get; set; }
        public int CurrencyID { get; set; }
        public int BaseCurrencyID { get; set; }
        public decimal BaseUnitsPerUnit { get; set; }
        public decimal? PreviousBaseUnitsPerUnit { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? ChangeReason { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}
