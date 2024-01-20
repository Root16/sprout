using Azure.Identity;
using Root16.Sprout.DependencyInjection;

namespace Root16.Sprout.Extensions;

public static class IEnumerableExtensions
{
    public async static Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> values)
    {
        var returnedValues = new List<T>();
        await foreach (var value in values)
        {
            returnedValues.Add(value);
        }
        return returnedValues;
    }

    internal async static IAsyncEnumerable<T> AsTasksComplete<T>(this IList<Task<T>> runningFunctions)
    {
        while (runningFunctions.Any())
        {
            Task<T> finishedTask = await Task.WhenAny(runningFunctions);
            runningFunctions.Remove(finishedTask);
            yield return await finishedTask;
        }
    }
    
}
