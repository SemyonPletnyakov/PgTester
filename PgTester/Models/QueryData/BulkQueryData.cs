
namespace PgTester.Models.QueryData;

public sealed class BulkQueryData : QueryData
{
    public BulkQueryData(string pathToQuery, string pathToData)
        : base(pathToQuery)
    {
        _pathToData = pathToData ?? throw new ArgumentNullException(nameof(pathToData));
    }

    public async Task<string> GetRawData(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        using var reader = new StreamReader(_pathToData);
        return await reader.ReadToEndAsync(token);
    }

    private readonly string _pathToData;
}
