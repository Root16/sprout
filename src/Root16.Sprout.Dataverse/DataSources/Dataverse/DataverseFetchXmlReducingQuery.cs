﻿using Microsoft.PowerPlatform.Dataverse.Client.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Xml.Linq;

namespace Root16.Sprout.DataSources.Dataverse;

public class DataverseFetchXmlReducingQuery(DataverseDataSource dataSource, string fetchXml, string? countByAttribute = null) : IPagedQuery<Entity>
{
    private readonly DataverseDataSource dataSource = dataSource;
    private readonly string fetchXml = fetchXml;
    private readonly string? countByAttribute = countByAttribute;
    private static string AddPaging(string fetchXml, int? page, int? pageSize, string? pagingCookie)
    {
        var fetchDoc = XDocument.Parse(fetchXml);
        AddPaging(fetchDoc, page, pageSize, pagingCookie);
        return fetchDoc.ToString(SaveOptions.DisableFormatting);
    }

    private static void AddPaging(XDocument fetchDoc, int? page, int? pageSize, string? pagingCookie)
    {
        if (page is not null)
            fetchDoc.Root?.SetAttributeValue("page", page);
        if (pageSize is not null)
            fetchDoc.Root?.SetAttributeValue("count", pageSize);
        if (pagingCookie is not null)
        {
            fetchDoc.Root?.SetAttributeValue("paging-cookie", pagingCookie);
        }
    }

    // results should be 'different' everytime cause reducing
    public async Task<PagedQueryResult<Entity>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark)
    {
        var results = await dataSource.CrmServiceClient.RetrieveMultipleWithRetryAsync(new FetchExpression(fetchXml)); 

        return new PagedQueryResult<Entity>
        (
            [.. results.Entities.Take(pageSize)],
            results.MoreRecords || results.Entities.Count > pageSize,
            results.PagingCookie
        );
    }

    public async Task<int?> GetTotalRecordCountAsync(int batchSize, int? maxBatchCount)
    {
        var fetchDoc = XDocument.Parse(fetchXml);
        if (fetchDoc.Root is null)
        {
            return null;
        }

        if ((bool?)fetchDoc.Root?.Attribute("aggregate") == true)
        {
            return null;
        }

        var entityElem = fetchDoc.Root?.Element("entity");
        if (entityElem is null)
        {
            return null;
        }

        var attributeElements = fetchDoc.Root?.Descendants().Where(e => e.Name == "attribute").ToArray();
        if (attributeElements is null)
        {
            return null;
        }

        foreach (var attr in attributeElements)
        {
            attr.Remove();
        }

        string? primaryAttribute = this.countByAttribute;
        if (string.IsNullOrWhiteSpace(primaryAttribute))
        {
            var entityMetadata = dataSource.CrmServiceClient.GetEntityMetadata(entityElem.Attribute("name")?.Value, EntityFilters.Entity);
            primaryAttribute = entityMetadata.PrimaryIdAttribute;
        }
        entityElem.Add(new XElement("attribute", new XAttribute("name", primaryAttribute)));

        int page = 1;

        bool moreRecords;
        string? pagingCookie = null;
        int pageSize = 5000;
        int totalCount = 0;

        do
        {
            AddPaging(fetchDoc, page, pageSize, pagingCookie);
            var results = await dataSource.CrmServiceClient.RetrieveMultipleWithRetryAsync(new FetchExpression { Query = fetchDoc.ToString(SaveOptions.DisableFormatting) });
            totalCount += results.Entities.Count;
            moreRecords = results.MoreRecords;
            pagingCookie = results.PagingCookie;
            page++;

            if(maxBatchCount is not null && totalCount >= (maxBatchCount * batchSize))
            {
                break;
            }
        }
        while (moreRecords);

        return maxBatchCount is null
            ? (totalCount)
            : (int?)Math.Min(totalCount, batchSize * maxBatchCount.Value);
    }
}