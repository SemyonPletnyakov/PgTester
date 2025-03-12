namespace PgTester.Abstractions.Logic;

public interface IQueriesExecuter : IDisposable
{
    public Task ExecuteAsync(CancellationToken token);
}
