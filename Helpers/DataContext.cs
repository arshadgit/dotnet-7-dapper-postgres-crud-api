
namespace WebApi.Helpers;

using System.Data;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

public class DataContext
{
    private readonly DbSettings _dbSettings;

    public DataContext(IOptions<DbSettings> dbSettings)
    {
        _dbSettings = dbSettings.Value;
    }

    // Create connection using Patroni-aware primary selection
    private NpgsqlConnection CreatePostgresConnection(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = _dbSettings.Hosts,
            Database = database,
            Username = _dbSettings.UserId,
            Password = _dbSettings.Password,

            // Only connect to a writable PostgreSQL primary
            TargetSessionAttributes = "read-write",

            Timeout = 5,
            CommandTimeout = 30,
            Pooling = true
        };

        return new NpgsqlConnection(builder.ConnectionString);
    }

    public IDbConnection CreateConnection()
    {
        return CreatePostgresConnection(_dbSettings.Database);
    }

    public async Task Init()
    {
        await _initDatabase();
        await _initTables();
    }

    private async Task _initDatabase()
    {
        using var connection =
            CreatePostgresConnection("postgres");

        await connection.OpenAsync();

        var sqlDbCount = """
            SELECT COUNT(*)
            FROM pg_database
            WHERE datname = @Database;
            """;

        var dbCount = await connection.ExecuteScalarAsync<int>(
            sqlDbCount,
            new { Database = _dbSettings.Database }
        );

        if (dbCount == 0)
        {
            var databaseName =
                _dbSettings.Database.Replace("\"", "\"\"");

            var sql = $"CREATE DATABASE \"{databaseName}\"";

            await connection.ExecuteAsync(sql);
        }
    }

    private async Task _initTables()
    {
        using var connection =
            CreatePostgresConnection(_dbSettings.Database);

        await connection.OpenAsync();

        var sql = """
            CREATE TABLE IF NOT EXISTS Users (
                Id SERIAL PRIMARY KEY,
                Title VARCHAR,
                FirstName VARCHAR,
                LastName VARCHAR,
                Email VARCHAR,
                Role INTEGER,
                PasswordHash VARCHAR
            );
            """;

        await connection.ExecuteAsync(sql);
    }
}
