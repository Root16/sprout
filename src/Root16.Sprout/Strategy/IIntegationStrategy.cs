using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Text;

namespace Root16.Sprout.Strategy;

public interface IIntegationStrategy
{
	void Migrate<TSource, TDest>(IIntegrationRuntime integration, IIntegrationStep<TSource, TDest> step);
}
