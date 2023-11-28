using Root16.Sprout.Processors;
using Root16.Sprout.Progress;
using System;
using System.Collections.Generic;

namespace Root16.Sprout.Data;

public interface IDataOperationEndpoint<T>
{
	Task<IReadOnlyList<DataOperationResult<T>>> PerformOperationsAsync(IEnumerable<DataOperation<T>> operations);
}
