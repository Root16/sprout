﻿using Root16.Sprout.DependencyInjection;

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

    internal async static IAsyncEnumerable<T> StreamFinishedTasksAllRunning<T>(this IList<Task<T>> runningFunctions)
    {
        while (runningFunctions.Any())
        {
            var finishedTask = await Task.WhenAny(runningFunctions);
            runningFunctions.Remove(finishedTask);
            yield return await finishedTask;
        }
    }

    internal async static IAsyncEnumerable<TOutput> StreamFinishedTasksWithSpecificAmount<TOutput>(this List<KeyValuePair<StepRegistration,Func<StepRegistration,Task<TOutput>>>> functionsToRun, int maxConcurrentAmount = default)
    {
        //Get a list of functions and the list should be at most the maxConcurrentAmount
        var batchOfFunctionsToRun = functionsToRun.Take(maxConcurrentAmount).ToList();
        //Start them and add them to the the list of running functions
        //x.Value is a function that takes a StepRegistration as input and x.Key is the step registration that is needed
        var runningFunctions = batchOfFunctionsToRun.Select(x => x.Value(x.Key)).ToList();
        //Remove the functions from the list of functionToRun only after they have been started
        batchOfFunctionsToRun.ForEach((x) => functionsToRun.Remove(x));

        while (runningFunctions.Any())
        {
            //When a task is finished return the value and then remove the task from running tasks and then find as many new tasks to start as the amount will allow. Start them and then remove the tasks from tasks to run.
            //Returning first allows the calling method to add more to the tasks before we do another check
            var finishedFunction = await Task.WhenAny(runningFunctions);
            yield return await finishedFunction;
            runningFunctions.Remove(finishedFunction);
            if (functionsToRun.Any())
            {
                var newBatchOfFunctionsToRun = functionsToRun.Take(maxConcurrentAmount - (runningFunctions.Count)).ToList();
                runningFunctions.AddRange(newBatchOfFunctionsToRun.Select(x => x.Value(x.Key)));
                newBatchOfFunctionsToRun.ForEach(x => functionsToRun.Remove(x));
            }
        }
    }
}