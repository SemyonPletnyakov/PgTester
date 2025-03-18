using PgTester.Abstractions.Logic;
using PgTester.Abstractions.Logic.Queries;
using PgTester.Models;

namespace PgTester.Logic;

public sealed class RepeatableExperiment : IExperiment
{
    public RepeatableExperiment(IExperiment experiment, int repeatCount)
    {
        _experiment = experiment 
            ?? throw new ArgumentNullException(nameof(experiment));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repeatCount);
        _repeatCount = repeatCount;
    }

    public async Task<Statistic> ExecuteAsync(
        IQueryFactory queryFactory,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var statistics = new Statistic[_repeatCount];

        for (int i = 0; i < _repeatCount; i++)
        {
            statistics[i] = await _experiment.ExecuteAsync(queryFactory, token);
        }

        return Statistic.GetAverageFromStatistics(statistics);
    }

    private readonly IExperiment _experiment;
    private readonly int _repeatCount;
}
