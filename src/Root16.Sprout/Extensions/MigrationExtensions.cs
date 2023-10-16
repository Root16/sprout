using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Root16.Sprout.Extensions;

public static class MigrationExtensions
{
	public static IMigrationBuilder AddSqlDataSource(this IMigrationBuilder builder, string connectionStringName)
	{
		//TODO
		//var connectionMigrationToolkitString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
		throw new NotImplementedException();
		return builder.AddSqlDataSource(connectionStringName, "");
	}

	public static IMigrationBuilder AddSqlDataSource(this IMigrationBuilder builder, string name, string connectionString)
	{
		return builder.AddDataSource(name, new SqlDataSource(connectionString, builder.CreateLogger<SqlDataSource>()));
	}

	public static IMigrationBuilder AddLoggingProgressListener(this IMigrationBuilder builder)
	{
		return builder.AddProgressListener(new LoggingProgressListener(builder.CreateLogger<LoggingProgressListener>()));
	}

	public static IMigrationBuilder AddConsoleProgressListener(this IMigrationBuilder builder)
	{
		return builder.AddProgressListener(new ConsoleProgressListener());
	}

	public static IMigrationBuilder AddStep<T>(this IMigrationBuilder builder, string name) where T : IMigrationStep
	{
		//TODO
		throw new NotImplementedException();
		//return builder.AddStep(name, (Action<T>)null);
	}

	public static IMigrationBuilder AddStep<T>(this IMigrationBuilder builder, string name, Action<T> configure) where T : IMigrationStep
	{
		//TODO
		throw new NotImplementedException();
		T step = (T)Activator.CreateInstance(typeof(T), builder.CreateLogger<T>());
		configure?.Invoke(step);
		//return builder.AddStep(name, step);
	}

	public static SqlDataSource GetSqlDataSource(this IMigrationRuntime runtime, string name)
	{
		return runtime.GetDataSource<SqlDataSource>(name);
	}

	public static SqlDataSource GetSqlDataSource(this IMigrationRuntime runtime)
	{
		return runtime.GetDataSource<SqlDataSource>();
	}

	public static T GetVariable<T>(this IMigrationRuntime runtime, string key)
	{
		if (runtime.Variables.TryGetValue(key, out object value))
		{
			return (T)value;
		}

		throw new KeyNotFoundException();
	}

	public static bool TryGetVariable<T>(this IMigrationRuntime runtime, string key, out T value)
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
