using System.Data;
using Microsoft.Data.SqlClient;

namespace HamgamTransport.Server.Services;

public interface ISqlConnectionFactory
{
    Task<IDbConnection> OpenAsync(CancellationToken cancellationToken = default);
}

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Local")
            ?? throw new InvalidOperationException("Connection string 'Local' یافت نشد.");
    }

    public async Task<IDbConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
