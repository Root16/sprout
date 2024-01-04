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

    internal async static IAsyncEnumerable<TOutput> StreamFinishedTasksWithSpecificAmount<TOutput>(this List<KeyValuePair<StepRegistration,Func<StepRegistration,Task<TOutput>>>> tasksToRun, int amount = default)
    {
        var runningTasks = new List<Task<TOutput>>();
        var newTasks = new List<KeyValuePair<StepRegistration, Func<StepRegistration, Task<TOutput>>>>();
        //Start a specific amount of tasks and then remove them from the list of tasks to run
        var TasksToRun = tasksToRun.Take(amount).ToList();
        TasksToRun.ForEach((x) => tasksToRun.Remove(x));
        runningTasks = TasksToRun.Select(x => x.Value(x.Key)).ToList();

        while (runningTasks.Any())
        {
            //When a task is finished return the value and then remove the task from running tasks and then find as many new tasks to start as the amount will allow. Start them and then remove the tasks from tasks to run.
            //Returning first allows the calling method to add more to the tasks before we do another check
            var finishedTask = await Task.WhenAny(runningTasks);
            var value = await finishedTask;
            yield return value;
            runningTasks.Remove(finishedTask);
            if (tasksToRun.Any())
            {
                newTasks = tasksToRun.Take(amount - (runningTasks.Count)).ToList();
                runningTasks.AddRange(newTasks.Select(x => x.Value(x.Key)));
                newTasks.ForEach(x => tasksToRun.Remove(x));
            }
        }
    }
}
