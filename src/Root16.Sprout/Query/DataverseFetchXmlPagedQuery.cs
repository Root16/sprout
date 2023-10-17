using Microsoft.PowerPlatform.Dataverse.Client.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Root16.Sprout.Query;

public class DataverseFetchXmlPagedQuery : IPagedQuery<Entity>
{
	private readonly DataverseDataSource dataSource;
	private readonly string fetchXml;
	private string pagingCookie = null;
	private int page = 1;
	private int pageSize;

	public DataverseFetchXmlPagedQuery(DataverseDataSource dataSource, string fetchXml)
	{
		this.dataSource = dataSource;
		this.fetchXml = fetchXml;
		MoreRecords = true;
	}

	public bool MoreRecords { get; private set; }

	public IReadOnlyList<Entity> GetNextPage(int pageSize)
	{
		if (page > 1 && pageSize != this.pageSize)
		{
			throw new NotImplementedException($"{nameof(DataverseFetchXmlPagedQuery)} does not support changing page size.");
		}

		this.pageSize = pageSize;
		var results = dataSource.CrmServiceClient.RetrieveMultiple(new FetchExpression(AddPaging(fetchXml, page, pageSize, pagingCookie)));

		MoreRecords = results.MoreRecords;
		pagingCookie = results.PagingCookie;
		page++;

		return results.Entities;
	}

	public int? GetTotalRecordCount()
	{
		var fetchDoc = XDocument.Parse(fetchXml);
		if ((bool?)fetchDoc.Root.Attribute("aggregate") == true)
		{
			return null;
		}

		var entityElem = fetchDoc.Root.Element("entity");
		if (entityElem == null)
		{
			return null;
		}

		var attributeElements = fetchDoc.Root.Descendants().Where(e => e.Name == "attribute").ToArray();
		foreach (var attr in attributeElements)
		{
			attr.Remove();
		}

		var entityMetadata = dataSource.CrmServiceClient.GetEntityMetadata(entityElem.Attribute("name").Value, EntityFilters.Entity);
		var primaryAttribute = entityMetadata.PrimaryIdAttribute;
		entityElem.Add(new XElement("attribute", new XAttribute("name", primaryAttribute)));

		int page = 1;

		bool moreRecords;
		string pagingCookie = null;
		int pageSize = 5000;
		int totalCount = 0;

		do
		{
			AddPaging(fetchDoc, page, pageSize, pagingCookie);
			var results = dataSource.CrmServiceClient.RetrieveMultiple(new FetchExpression { Query = fetchDoc.ToString(SaveOptions.DisableFormatting) });
			totalCount += results.Entities.Count;
			moreRecords = results.MoreRecords;
			pagingCookie = results.PagingCookie;
			page++;
		}
		while (moreRecords);

		return totalCount;
	}

	private static string AddPaging(string fetchXml, int page, int pageSize, string pagingCookie)
	{
		var fetchDoc = XDocument.Parse(fetchXml);
		AddPaging(fetchDoc, page, pageSize, pagingCookie);
		return fetchDoc.ToString(SaveOptions.DisableFormatting);
	}

	private static void AddPaging(XDocument fetchDoc, int page, int pageSize, string pagingCookie)
	{
		fetchDoc.Root.SetAttributeValue("page", page);
		fetchDoc.Root.SetAttributeValue("count", pageSize);
		if (pagingCookie != null)
		{
			fetchDoc.Root.SetAttributeValue("paging-cookie", pagingCookie);
		}
	}
}
