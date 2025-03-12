using PgTester.Models;

namespace PgTester.Abstractions.Logic;

public interface IExperiment : IDisposable
{
    public Task<Statistic> Execute(CancellationToken cancellationToken);
}
