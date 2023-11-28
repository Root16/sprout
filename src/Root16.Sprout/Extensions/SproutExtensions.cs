using Microsoft.Xrm.Sdk;
using Root16.Sprout.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Extensions;

public static class SproutExtensions
{
	//public static IIntegrationBuilder AddDataverseDataSource(this IIntegrationBuilder builder, string connectionStringName)
	//{
	//	var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
	//	return builder.AddDataverseDataSource(connectionStringName, connectionString);
	//}

	//public static IIntegrationBuilder AddDataverseDataSource(this IIntegrationBuilder builder, string name, string connectionString)
	//{
	//	var ds = new DataverseDataSource(connectionString, builder.CreateLogger<DataverseDataSource>());
	//	return builder.AddDataSource(name, ds);
	//}

	//public static DataverseDataSource GetDataverseDataSource(this IIntegrationRuntime runtime, string name)
	//{
	//	return runtime.GetDataSource<DataverseDataSource>(name);
	//}

	//public static DataverseDataSource GetDataverseDataSource(this IIntegrationRuntime runtime)
	//{
	//	return runtime.GetDataSource<DataverseDataSource>();
	//}

	public static Entity GetDelta(this Entity updates, Entity original)
	{
		Entity delta = new Entity(original.LogicalName, original.Id);
		foreach (var attribute in updates.Attributes)
		{
			bool different = false;
			original.Attributes.TryGetValue(attribute.Key, out object originalValue);
			var updateValue = attribute.Value;

			if (updateValue is EntityReference || originalValue is EntityReference)
			{
				var originalLookup = (EntityReference)originalValue;
				var updateLookup = (EntityReference)updateValue;

				if (updateLookup?.Id != originalLookup?.Id ||
					updateLookup?.LogicalName != originalLookup?.LogicalName)
				{
					different = true;
				}
			}
			else if (updateValue is Money || originalValue is Money)
			{
				var originalMoney = (Money)originalValue;
				var updateMoney = (Money)updateValue;

				if (updateMoney?.Value != originalMoney?.Value)
				{
					different = true;
				}
			}
			else if (updateValue is OptionSetValue || originalValue is OptionSetValue)
			{
				var originalOptionSetValue = (OptionSetValue)originalValue;
				var updateOptionSetValue = (OptionSetValue)updateValue;

				if (updateOptionSetValue?.Value != originalOptionSetValue?.Value)
				{
					different = true;
				}
			}
			else if (updateValue is OptionSetValueCollection || originalValue is OptionSetValueCollection)
			{
				var originalOptionSetValue = (OptionSetValueCollection)originalValue;
				var updateOptionSetValue = (OptionSetValueCollection)updateValue;
				var originalOptions = originalOptionSetValue?.Select(o => o.Value)?.ToArray() ?? Array.Empty<int>();
				var updateOptions = updateOptionSetValue?.Select(o => o.Value)?.ToArray() ?? Array.Empty<int>();

				if (originalOptions.Length != updateOptions.Length ||
					originalOptions.Intersect(updateOptions).Count() != originalOptions.Length)
				{
					different = true;
				}
			}
			else if (updateValue is string || originalValue is string)
			{
				if ((string)updateValue == "" && originalValue == null)
				{
					different = false;
				}
				else if (!Object.Equals(updateValue, originalValue))
				{
					different = true;
				}
			}
			else
			{
				if (updateValue is DateTime)
				{
					var dt = (DateTime)updateValue;
					updateValue = (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind)).ToUniversalTime();
					if (originalValue is DateTime)
					{
						originalValue = ((DateTime)originalValue).ToUniversalTime();
					}
				}

				if (!Object.Equals(updateValue, originalValue))
				{
					different = true;
				}
			}


			if (different)
			{
				delta[attribute.Key] = updateValue;
			}
		}
		return delta;
	}

	public static string FormatChanges(this Entity entity, Entity previousValues)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"updating ({entity.LogicalName}, {entity.Id}):");
		foreach (var attribute in entity.Attributes)
		{
			previousValues.Attributes.TryGetValue(attribute.Key, out object matchValue);
			sb.AppendLine($"    {attribute.Key}: {DisplayAttributeValue(matchValue)} => {DisplayAttributeValue(attribute.Value)}");
		}

		return sb.ToString();
	}

	public static object DisplayAttributeValue(object attributeValue)
	{
		if (attributeValue == null)
		{
			return "(null)";
		}
		else if (attributeValue is EntityReference)
		{
			var entityRef = (EntityReference)attributeValue;
			return $"({entityRef.LogicalName}, {entityRef.Id})";
		}
		else if (attributeValue is Money)
		{
			var money = (Money)attributeValue;
			return money.Value;
		}
		else if (attributeValue is OptionSetValue)
		{
			var optionSetValue = (OptionSetValue)attributeValue;
			return optionSetValue.Value;
		}
		else if (attributeValue is string)
		{
			return $"'{attributeValue}'";
		}
		else
		{
			return attributeValue;
		}
	}
}
