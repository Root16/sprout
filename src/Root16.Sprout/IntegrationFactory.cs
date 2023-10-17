using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Root16.Sprout;

public static class IntegrationFactory
{
	public static IIntegrationRuntime Create(Action<IIntegrationBuilder> configure)
	{
		var builder = new Integration();
		configure?.Invoke(builder);
		return builder.Create();
	}
}
