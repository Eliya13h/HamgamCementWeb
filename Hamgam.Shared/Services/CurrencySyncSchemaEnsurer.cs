using Microsoft.Data.SqlClient;

namespace Hamgam.Shared.Services;

public static class CurrencySyncSchemaEnsurer
{
    public static async Task EnsureLocalCurrencyColumnsAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            IF COL_LENGTH('Currencies', 'UseInBothSystems') IS NULL
                ALTER TABLE Currencies ADD UseInBothSystems bit NOT NULL CONSTRAINT DF_Currencies_UseInBothSystems DEFAULT(0);
            IF COL_LENGTH('Currencies', 'OriginSystem') IS NULL
                ALTER TABLE Currencies ADD OriginSystem nvarchar(20) NOT NULL CONSTRAINT DF_Currencies_OriginSystem DEFAULT('');
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
