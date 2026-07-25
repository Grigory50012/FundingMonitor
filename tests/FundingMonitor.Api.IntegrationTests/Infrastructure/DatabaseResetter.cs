using Npgsql;
using Respawn;
using Respawn.Graph;

namespace FundingMonitor.Api.IntegrationTests.Infrastructure;

public sealed class DatabaseResetter
{
    private readonly string _connectionString;
    private readonly Respawner _respawner;

    private DatabaseResetter(string connectionString, Respawner respawner)
    {
        _connectionString = connectionString;
        _respawner = respawner;
    }

    public static async Task<DatabaseResetter> CreateAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = [new Table("public", "__EFMigrationsHistory")]
            });

        return new DatabaseResetter(connectionString, respawner);
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}
