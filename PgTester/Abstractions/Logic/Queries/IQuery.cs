namespace PgTester.Abstractions.Logic.Queries;

public interface IQuery : IDisposable
{
    public Task ExecuteAsync(CancellationToken token);
}
