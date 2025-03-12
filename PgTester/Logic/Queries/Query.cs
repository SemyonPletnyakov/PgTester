using Npgsql;
using PgTester.Abstractions.Logic.Queries;

namespace PgTester.Logic.Queries;

public sealed class Query : IQuery
{
    public Query(NpgsqlConnection connection, string query)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        using var command = new NpgsqlCommand(_query, _connection);
        command.CommandTimeout = 1_000_000;
        await command.ExecuteNonQueryAsync(token);
    }

    public void Dispose() => _connection.Dispose();

    private NpgsqlConnection _connection;
    private string _query;
}
