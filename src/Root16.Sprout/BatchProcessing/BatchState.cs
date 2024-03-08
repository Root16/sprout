using Root16.Sprout.DataStores;
using Root16.Sprout.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.BatchProcessing;

public record BatchState<TInput>(PagedQueryState<TInput>? QueryState, IntegrationProgress? Progress);
