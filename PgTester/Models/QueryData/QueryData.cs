namespace PgTester.Models.QueryData;

public class QueryData
{
    public QueryData(string pathToQuery)
    {
        _pathToQuery = 
            pathToQuery ?? throw new ArgumentNullException(nameof(pathToQuery));
    }

    public async Task<string> GetRawQuery(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        using var reader = new StreamReader(_pathToQuery);
        return await reader.ReadToEndAsync(token);
    }

    private string _pathToQuery;
}
