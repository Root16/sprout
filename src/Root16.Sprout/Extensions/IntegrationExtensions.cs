using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Root16.Sprout.Extensions;

public static class IntegrationExtensions
{
	public static IIntegrationBuilder AddSqlDataSource(this IIntegrationBuilder builder, string connectionStringName)
	{
		//TODO
		//var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
		//return builder.AddSqlDataSource(connectionStringName, connectionString);
		throw new NotImplementedException();
	}

	public static IIntegrationBuilder AddSqlDataSource(this IIntegrationBuilder builder, string name, string connectionString)
	{
		return builder.AddDataSource(name, new SqlDataSource(connectionString, builder.CreateLogger<SqlDataSource>()));
	}

	public static IIntegrationBuilder AddLoggingProgressListener(this IIntegrationBuilder builder)
	{
		return builder.AddProgressListener(new LoggingProgressListener(builder.CreateLogger<LoggingProgressListener>()));
	}

	public static IIntegrationBuilder AddConsoleProgressListener(this IIntegrationBuilder builder)
	{
		return builder.AddProgressListener(new ConsoleProgressListener());
	}

	public static IIntegrationBuilder AddStep<T>(this IIntegrationBuilder builder, string name) where T : IIntegrationStep
	{
		//TODO
		throw new NotImplementedException();
		//return builder.AddStep(name, (Action<T>)null);
	}

	public static IIntegrationBuilder AddStep<T>(this IIntegrationBuilder builder, string name, Action<T> configure) where T : IIntegrationStep
	{
		//TODO
		throw new NotImplementedException();
		T step = (T)Activator.CreateInstance(typeof(T), builder.CreateLogger<T>());
		configure?.Invoke(step);
		//return builder.AddStep(name, step);
	}

	public static SqlDataSource GetSqlDataSource(this IIntegrationRuntime runtime, string name)
	{
		return runtime.GetDataSource<SqlDataSource>(name);
	}

	public static SqlDataSource GetSqlDataSource(this IIntegrationRuntime runtime)
	{
		return runtime.GetDataSource<SqlDataSource>();
	}

	public static T GetVariable<T>(this IIntegrationRuntime runtime, string key)
	{
		if (runtime.Variables.TryGetValue(key, out object value))
		{
			return (T)value;
		}

		throw new KeyNotFoundException();
	}

	public static bool TryGetVariable<T>(this IIntegrationRuntime runtime, string key, out T value)
	{
		if (runtime.Variables.TryGetValue(key, out object obj))
		{
			value = (T)obj;
			return true;
		}

		value = default(T);
		return false;
	}
}
