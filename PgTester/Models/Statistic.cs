namespace PgTester.Models;

public record Statistic
{
    public TimeSpan ClientTimeDuraction { get; }

    public Statistic(TimeSpan clientTimeDuraction)
    {
        if (clientTimeDuraction < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                $"ClientTimeDuraction cannot be negative value: {clientTimeDuraction}");
        }

        ClientTimeDuraction = clientTimeDuraction;
    }

    public static Statistic GetAverageFromStatistics(IReadOnlyCollection<Statistic> statistics)
    {
        var sumClientTimeDuraction = TimeSpan.Zero;
        
        foreach (Statistic statistic in statistics)
        {
            sumClientTimeDuraction += statistic.ClientTimeDuraction;
        }

        var averageClientTimeDuraction = sumClientTimeDuraction / statistics.Count;

        return new(averageClientTimeDuraction);
    }
}
