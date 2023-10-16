using Root16.Sprout.Progress;
using System;
using System.Collections.Generic;

namespace Root16.Sprout.Data;

public interface IDataSink<TDest>
{
	IReadOnlyList<DataChangeType> Update(IEnumerable<DataChange<TDest>> dests);
}