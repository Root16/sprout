using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Extensions;
using Microsoft.Xrm.Sdk.Metadata;
using Root16.Sprout.Utilities.Dataverse.ColumnAnalyzer;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

/* TODO:
- identify system-managed columns e.g. pricing data, if possible
- optionally evaluate table dependencies and identify circular references
*/

IConfiguration config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddJsonFile("appsettings.json", true)
    .Build();

Settings settings = config.GetRequiredSection(nameof(Settings)).Get<Settings>();

ServiceClient dataverse = new(config.GetConnectionString("Dataverse")
    ?? throw new InvalidConfigurationException("Missing required connection string key \"Dataverse\"!"));

if (settings.GetRowCounts || settings.GetColumnUsageCounts)
{
    if (string.IsNullOrWhiteSpace(config.GetConnectionString("TDS")))
    {
        throw new InvalidConfigurationException("Missing required connection string key \"TDS\"!");
    }
}

List<string> tables = settings.TableLogicalNames
    .Select(e => e.Trim().ToLower())
    .Distinct()
    .Order()
    .ToList();

Console.WriteLine($"Outputs will be written to folder: {settings.FolderPath}");

Console.WriteLine("Processing tables:");
foreach (var table in tables)
{
    Console.WriteLine($"- {table}");
}
Console.WriteLine();

Console.WriteLine("Retrieving all table metadata from Dataverse...");
var entityMetadataMap = dataverse.GetAllEntityMetadata(filter: EntityFilters.Entity | EntityFilters.Attributes)
    .ToDictionary(e => e.LogicalName);
Console.WriteLine("Finished retrieving metadata.\n");

var missingTables = settings.TableLogicalNames
    .Where(e => !entityMetadataMap.ContainsKey(e))
    .ToList();

if (missingTables.Count > 0)
{
    Console.WriteLine("The following tables were not found in your Dataverse environment!");
    foreach (var table in missingTables)
    {
        Console.WriteLine($"- {table}");
    }
    throw new Exception("Resolve or remove the missing tables, then try again.");
}

if (settings.GetRowCounts)
{
    Console.WriteLine("Retrieving rowcounts using TDS...");

    List<TableRowCount> rowcounts = [];
    
    Parallel.ForEach(tables, table =>
    {
        var tds = new SqlConnection(config.GetConnectionString("TDS"));
        tds.Open();
        var query = new SqlCommand($@"SELECT COUNT(*) FROM {table}", tds);
        object? result = query.ExecuteScalar();
        var rowcount = (int)result;
        Console.CursorLeft = 0;
        Console.WriteLine($"{table} has {rowcount} rows");
        rowcounts.Add(new TableRowCount
        {
            Table = table,
            RowCount = rowcount
        });
        tds.Close();
    });

    using var rcWriter = new StreamWriter($"{settings.FolderPath}\\table-rowcounts.csv");
    using var rcCsv = new CsvWriter(rcWriter, CultureInfo.InvariantCulture);
    rcCsv.WriteRecords(rowcounts);
}

List<string> ExcludedColumns =
[
    "*_base",
    "*idtype",
    "*yominame",
    "importsequencenumber",
    "parentcustomeridtype",
    "processid",
    "regardingobjectidname",
    "regardingobjecttypecode",
    "slaid",
    "stageid",
    "timezoneruleversionnumber",
    "traversedpath",
    "utcconversiontimezonecode",
    "yomi*",
];

ExcludedColumns.AddRange(settings.ExcludePrefixes.Select(prefix => $"{(prefix.EndsWith('_') ? prefix : prefix + '_')}*"));

List<TableColumnOutput> tableColumns = [];

foreach (var table in tables)
{
    var metadata = entityMetadataMap[table];
    var columns = metadata.Attributes.ToList();

    foreach (var col in columns)
    {
        TableColumnOutput output = new()
        {
            Table = table,
            Column = col.LogicalName,
            ColumnLabel = col.DisplayName?.UserLocalizedLabel?.Label,
            AttributeType = col.AttributeTypeName.Value,
            IsValidForCreate = col.IsValidForCreate.GetValueOrDefault(),
            IsValidForUpdate = col.IsValidForUpdate.GetValueOrDefault(),
            IsValidForRead = col.IsValidForRead.GetValueOrDefault(),
            CalculationType = col.SourceType.GetValueOrDefault() == 0 ? null
                : col.SourceType == 1 ? "Calculated"
                : col.SourceType == 2 ? "Rollup"
                : col.SourceType.ToString() // Formula?
        };

        foreach (var exclude in ExcludedColumns)
        {
            var key = exclude.Trim('*');
            if (exclude.StartsWith('*') && exclude.EndsWith('*') && col.LogicalName.Contains(key))
            {
                output.ExcludedBy = exclude;
            }
            else if (exclude.StartsWith('*') && col.LogicalName.EndsWith(key))
            {
                output.ExcludedBy = exclude;
            }
            else if (exclude.EndsWith('*') && col.LogicalName.StartsWith(key))
            {
                output.ExcludedBy = exclude;
            }
            else if (col.LogicalName == key)
            {
                output.ExcludedBy = "Exact match";
            }
        }

        if (col.LogicalName == "createdon" && columns.Any(c => c.LogicalName == "overriddencreatedon"))
        {
            output.IsIncluded = true;
            output.Note = "Recommend setting overriddencreatedon to this on create";
        }
        else if (col.LogicalName == "overriddencreatedon")
        {
            output.IsIncluded = false;
            output.Note = "Recommend setting this to createdon on create";
        }
        else if (!string.IsNullOrEmpty(col.DeprecatedVersion))
        {
            output.IsIncluded = false;
            output.Note = "Column is platform deprecated";
        }
        else if (metadata.IsIntersect.GetValueOrDefault() && col.AttributeType == AttributeTypeCode.Uniqueidentifier)
        {
            output.IsIncluded = true;
        }
        else
        {
            output.IsIncluded = (output.IsValidForCreate || output.IsValidForUpdate) 
                && output.IsValidForRead 
                && output.ExcludedBy == null 
                && output.CalculationType == null;

            if (output.IsIncluded && col is LookupAttributeMetadata lookup)
            {
                var missingTargets = lookup.Targets.Where(e => !tables.Contains(e)).Order();
                if (missingTargets.Any())
                {
                    output.Note = $"WARN: Lookup table(s) not mapped include {string.Join("; ", missingTargets)}";
                }
            }
        }

        tableColumns.Add(output);
    }
}

if (settings.GetColumnUsageCounts)
{
    Console.WriteLine("\nGetting column usage counts...");

    var metadataMap = entityMetadataMap
        .SelectMany(e => e.Value.Attributes)
        .ToDictionary(e => (e.EntityLogicalName, e.LogicalName));

    const int CHUNK_SIZE = 2;
    var columnsToProcess = tableColumns
        .Where(e => e.IsIncluded
            && entityMetadataMap[e.Table].IsIntersect != true
            && metadataMap[(e.Table, e.Column)].AttributeType != AttributeTypeCode.PartyList)
        .GroupBy(e => e.Table)
        .SelectMany(grp => grp.Chunk(CHUNK_SIZE))
        .OrderBy(chunk => chunk[0].Column)
        .ToList();

    var totalByTable = columnsToProcess.SelectMany(e => e).GroupBy(e => e.Table)
        .ToDictionary(e => e.Key, e => e.Count());

    ConcurrentDictionary<string, int> countByTable = [];

    Lock _consoleLock = new();

    Parallel.ForEach(columnsToProcess, columns =>
    {
        var tds = new SqlConnection(config.GetConnectionString("TDS"));
        tds.Open();
        var table = columns[0].Table;
        var currentCount = countByTable.AddOrUpdate(table, (_) => 0, (_, prev) => prev);

        lock (_consoleLock)
        {
            Console.WriteLine($"{table}: processing {currentCount} of {totalByTable[table]}...");
        }

        try
        {
            var exprs = columns.Select(col => $"COUNT(CASE WHEN {col.Column} IS NOT NULL THEN 1 ELSE NULL END)");
            var query = new SqlCommand($"SELECT {string.Join(',', exprs)} FROM {table}", tds);
            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                for (var j = 0; j < columns.Length; j++)
                {
                    var columncount = reader.GetInt32(j);
                    columns[j].ColumnCount = columncount;
                    if (columncount == 0)
                    {
                        columns[j].IsIncluded = false;
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"\nFailed to get column count for columns:");
                foreach (var col in columns)
                {
                    Console.WriteLine($"- {col.Column}");
                    col.ColumnCount = -1;
                    col.Note = "Failed to get column count";
                }
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }
        }

        tds.Close();

        lock (_consoleLock)
        {
            currentCount = countByTable.AddOrUpdate(table, (_) => 0, (_, prev) => prev + columns.Length);
            Console.WriteLine($"{table}: processed {currentCount} of {totalByTable[table]}...");
        }
    });

    Console.WriteLine("\nQuerying PartyList column counts for activity tables...");
    Dictionary<int, string> ParticipationTypeMap = new()
    {
        [1] = "from",
        [2] = "to",
        [3] = "cc",
        [4] = "bcc",
        [5] = "requiredattendees",
        [6] = "optionalattendees",
        [7] = "organizer",
        [8] = "regardingobjectid",
        [9] = "ownerid",
        [10] = "resources",
        [11] = "customers",
        [12] = "???", // Chat Participant
        [13] = "related",
    };

    var partyLists = tableColumns
        .Where(e => e.IsIncluded && metadataMap[(e.Table, e.Column)].AttributeType == AttributeTypeCode.PartyList);

    var activityTypeCodes = partyLists
        .Select(e => e.Table)
        .Distinct()
        .ToDictionary(e => entityMetadataMap[e].ObjectTypeCode!.Value, e => e);

    var lookupTypeCodes = partyLists
        .SelectMany(e => (metadataMap[(e.Table, e.Column)] as LookupAttributeMetadata)!.Targets)
        .Distinct()
        .Where(e => tables.Contains(e))
        .ToDictionary(e => entityMetadataMap[e].ObjectTypeCode!.Value, e => e);

    var partyTds = new SqlConnection(config.GetConnectionString("TDS"));
    partyTds.Open();
    var partyQuery = new SqlCommand($@"
        SELECT a.activitytypecode,
            ap.participationtypemask,
            TotalRows = COUNT(*)
        FROM activityparty ap
        JOIN activitypointer a ON a.activityid = ap.activityid
        WHERE a.activitytypecode IN ({string.Join(',', activityTypeCodes.Keys)}) AND
            ap.partyobjecttypecode IN (0,8,{string.Join(',', lookupTypeCodes.Keys)})
        GROUP BY a.activitytypecode, ap.participationtypemask", partyTds);
    using var reader = partyQuery.ExecuteReader();
    while (reader.Read())
    {
        var activityTypeCode = reader.GetInt32(0);
        var participationTypeMask = reader.GetInt32(1);
        var rowcount = reader.GetInt32(2);

        if (!activityTypeCodes.TryGetValue(activityTypeCode, out string? activity)
            || !ParticipationTypeMap.TryGetValue(participationTypeMask, out string? column))
        {
            Console.WriteLine($"Unmapped partylist count - ActivityTypeCode {activityTypeCode}; Participation Type {participationTypeMask}; Total Count {rowcount}");
        }
        else
        {
            var col = tableColumns.SingleOrDefault(e => e.Table == activity && e.Column == column);
            if (col == null)
            {
                Console.WriteLine($"Unmapped partylist count - Activity {activity} ({activityTypeCode}); Column {column} ({participationTypeMask}); Total Count {rowcount}");
            }
            else
            {
                col.ColumnCount = rowcount;
            }
        }
    }
    partyTds.Close();
    Console.WriteLine("Finished querying PartyLists");

    foreach (var column in partyLists)
    {
        if (column.ColumnCount == null)
        {
            column.IsIncluded = false;
            if ((column.Note ?? "").StartsWith("WARN"))
            {
                column.Note = null;
            }
        }
    }
}

using var writer = new StreamWriter($"{settings.FolderPath}\\table-columns-output.csv");
using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
csv.WriteRecords(tableColumns);

var minimalOutput = tableColumns
    .Where(e => e.IsIncluded)
    .GroupBy(e => e.Table)
    .ToDictionary(e => e.Key, e => e.Select(col => col.Column).ToList());
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true
};
File.WriteAllText($"{settings.FolderPath}\\table-columns-minimal.json", JsonSerializer.Serialize(minimalOutput, jsonOptions));
