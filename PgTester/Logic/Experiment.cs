using PgTester.Abstractions.Logic;
using PgTester.Models;

namespace PgTester.Logic;

public sealed class Experiment
{
    public Experiment(
        IStatisticCollector statisticCollector, 
        IQueriesExecuter preparatoryQueries, 
        IQueriesExecuter targetQueries)
    {
        _statisticCollector = 
            statisticCollector ?? throw new ArgumentNullException(nameof(statisticCollector));
        _preparatoryQueries = 
            preparatoryQueries ?? throw new ArgumentNullException(nameof(preparatoryQueries));
        _targetQueries = targetQueries ?? throw new ArgumentNullException(nameof(targetQueries));
    }

    public async Task<Statistic> Execute(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _preparatoryQueries.ExecuteAsync(cancellationToken);

        _statisticCollector.Start();
        await _targetQueries.ExecuteAsync(cancellationToken);
        _statisticCollector.Stop();

        return _statisticCollector.GetStatistic();
    }

    public void Dispose()
    {
        _preparatoryQueries.Dispose();
        _targetQueries.Dispose();
    }

    private IStatisticCollector _statisticCollector;
    private IQueriesExecuter _preparatoryQueries;
    private IQueriesExecuter _targetQueries;
}
