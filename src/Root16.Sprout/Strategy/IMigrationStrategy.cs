using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Text;

namespace Root16.Sprout.Strategy;

public interface IMigrationStrategy
{
	void Migrate<TSource, TDest>(IMigrationRuntime migration, IMigrationStep<TSource, TDest> step);
}
