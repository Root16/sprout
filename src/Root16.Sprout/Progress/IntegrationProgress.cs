﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Root16.Sprout.Progress;

public class IntegrationProgress
{
	public IntegrationProgress(string stepName, int? totalRecordCount)
	{
		StepName = stepName;
        operationCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        TotalRecordCount = totalRecordCount;
		StartTime = DateTime.Now;
	}

	private readonly Dictionary<string,int> operationCounts;


	public string StepName { get; private set; }
	public int? TotalRecordCount { get; private set; }
	public int ProcessedRecordCount { get; private set; }
	public DateTime StartTime { get; private set; }
	public TimeSpan RunningTime { get { return DateTime.Now - StartTime; } }
	public TimeSpan? EstimatedRemainingTime
	{
		get
		{
			if (ProcessedRecordCount == 0 || TotalRecordCount == 0 || TotalRecordCount is null)
			{
				return null;
			}

			var pctComplete = ProcessedRecordCount / (double)TotalRecordCount;
			var runningTime = RunningTime;
			return TimeSpan.FromMilliseconds(runningTime.TotalMilliseconds / pctComplete - runningTime.TotalMilliseconds);
		}
	}

	public void AddOperations(int processedRecordCount, IEnumerable<string> operations)
	{
		ProcessedRecordCount += processedRecordCount;
		foreach (var operationGroup in operations.GroupBy(o => o, StringComparer.OrdinalIgnoreCase))
		{
			if (operationCounts.ContainsKey(operationGroup.Key))
			{
				operationCounts[operationGroup.Key] += operationGroup.Count();
			}
			else
			{
                operationCounts[operationGroup.Key] = operationGroup.Count();
            }
		}
	}

	public override string ToString()
	{
		StringBuilder message = new();
		message.Append($"{this.StepName}: ");

		if (TotalRecordCount > 0)
		{
			message.Append($"{ProcessedRecordCount}/{this.TotalRecordCount} ({Math.Floor(100.0 * (ProcessedRecordCount) / this.TotalRecordCount.Value)}%) ");
		}
		else
		{
			message.Append($"{ProcessedRecordCount} ");
		}

		if (this.EstimatedRemainingTime is not null)
		{
			var ts = this.EstimatedRemainingTime.Value;
			var values = new[]
			{
				new {Interval = ts.Days, Label = "d"},
				new {Interval = ts.Hours, Label = "h"},
				new {Interval = ts.Minutes, Label = "m"},
				new {Interval = ts.Seconds, Label = "s"},
			};

			bool nonZeroFound = false;
			for (int i = 0; i < values.Length - 1; i++)
			{
				if (!nonZeroFound && values[i].Interval > 0) nonZeroFound = true;

				if (nonZeroFound)
				{
					message.Append($"{values[i].Interval}{values[i].Label}, ");
				}
			}

			message.Append($"{values[values.Length - 1].Interval}{values[values.Length - 1].Label} remaining ");
		}

		message.Append(
			string.Join(", ", operationCounts.Select(pair => $"{pair.Key}: {pair.Value}")));
		return message.ToString();
	}
}
