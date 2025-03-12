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
}
