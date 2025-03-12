using PgTester.Abstractions.Logic.Queries;

namespace PgTester.Logic;

public sealed class QueriesExecuter : IQuery
{
    public QueriesExecuter(IEnumerable<IQuery> queries)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        foreach (var query in _queries)
        {
            await query.ExecuteAsync(token);
        }
    }

    public void Dispose()
    {
        foreach (var query in _queries)
        {
            query.Dispose();
        }
    }

    private IEnumerable<IQuery> _queries;
}
