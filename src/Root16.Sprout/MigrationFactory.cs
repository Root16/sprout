using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Root16.Sprout;

public static class MigrationFactory
{
	public static IMigrationRuntime Create(Action<IMigrationBuilder> configure)
	{
		var builder = new Migration();
		configure?.Invoke(builder);
		return builder.Create();
	}
}
