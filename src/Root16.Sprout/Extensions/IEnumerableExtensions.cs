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

    internal async static IAsyncEnumerable<T> StreamFinishedTasksAllRunning<T>(this IList<Task<T>> tasks)
    {
        while (tasks.Any())
        {
            var finishedTask = await Task.WhenAny(tasks);
            tasks.Remove(finishedTask);
            yield return await finishedTask;
        }
    }

    internal async static IAsyncEnumerable<TOutput> StreamFinishedTasksWithSpecificAmount<TOutput>(this List<KeyValuePair<StepRegistration,Func<StepRegistration,Task<TOutput>>>> tasksToRun, int amount = 5)
    {
        //Start a specific amount of tasks and then remove them from the list of tasks to run
        var TasksToRun = tasksToRun.Take(amount).ToList();
        TasksToRun.ForEach((x) => tasksToRun.Remove(x));
        var runningTasks = TasksToRun.Select(x => x.Value(x.Key)).ToList();

        while (runningTasks.Any())
        {
            //When a task has finished when return the value, find another task to add, add that new task, and then remove the finished task from the list of running tasks
            //Returning first allows the calling method to add more to the tasks before we do another check
            var finishedTask = await Task.WhenAny(runningTasks);
            var value = await finishedTask;
            yield return value;
            if (tasksToRun.Any())
            {
                var newTaskToRun = tasksToRun.FirstOrDefault();
                runningTasks.Add(newTaskToRun.Value(newTaskToRun.Key));
                tasksToRun.Remove(newTaskToRun);
            }
            runningTasks.Remove(finishedTask);
        }
    }
}
