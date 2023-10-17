using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Root16.Sprout.Progress;

public class IntegrationProgress
{
	public IntegrationProgress(string stepName, int? totalRecordCount)
	{
		StepName = stepName;
		createCount = 0;
		updateCount = 0;
		errorCount = 0;
		recordCount = 0;
		skipCount = 0;
		TotalRecordCount = totalRecordCount;
		StartTime = DateTime.Now;
	}

	private int createCount;
	private int updateCount;
	private int errorCount;
	private int recordCount;
	private int skipCount;


	public string StepName { get; private set; }
	public int CreateCount { get { return createCount; } }
	public int UpdateCount { get { return updateCount; } }
	public int ErrorCount { get { return errorCount; } }
	public int RecordCount { get { return recordCount; } }
	public int SkipCount { get { return skipCount; } }
	public int? TotalRecordCount { get; private set; }
	public DateTime StartTime { get; private set; }
	public TimeSpan RunningTime { get { return DateTime.Now - StartTime; } }
	public TimeSpan? EstimatedRemainingTime
	{
		get
		{
			if ((SkipCount + RecordCount) == 0 || TotalRecordCount == 0 || TotalRecordCount == null)
			{
				return null;
			}

			var pctComplete = (SkipCount + RecordCount) / (double)TotalRecordCount;
			var runningTime = RunningTime;
			return TimeSpan.FromMilliseconds(runningTime.TotalMilliseconds / pctComplete - runningTime.TotalMilliseconds);
		}
	}

	internal void AddSkippedRecords(int skipCount)
	{
		Interlocked.Add(ref this.skipCount, skipCount);
	}

	public void AddResult(DataChangeType result)
	{
		Interlocked.Increment(ref this.recordCount);
		switch (result)
		{
			case DataChangeType.Error:
				Interlocked.Increment(ref errorCount);
				break;
			case DataChangeType.Create:
				Interlocked.Increment(ref createCount);
				break;
			case DataChangeType.Update:
				Interlocked.Increment(ref updateCount);
				break;
			default:
				break;
		}
	}

	public void AddResultRange(IEnumerable<DataChangeType> results)
	{
		foreach (var result in results)
		{
			AddResult(result);
		}
	}

	public override string ToString()
	{
		StringBuilder message = new StringBuilder();
		message.Append($"{this.StepName}: ");

		if (TotalRecordCount > 0)
		{
			message.Append($"{this.RecordCount + SkipCount}/{this.TotalRecordCount} ({Math.Floor(100.0 * (this.RecordCount + this.SkipCount) / this.TotalRecordCount.Value)}%) ");
		}
		else
		{
			message.Append($"{this.RecordCount + SkipCount} ");
		}

		if (this.EstimatedRemainingTime != null)
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

		message.Append($"({SkipCount} skipped, {this.CreateCount} created, {this.UpdateCount} updated, {this.ErrorCount} errors)");
		return message.ToString();
	}
}
