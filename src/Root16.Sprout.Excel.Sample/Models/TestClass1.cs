using Root16.Sprout.Excel.Extensions;

namespace Root16.Sprout.Excel.Sample.Models;

public class TestClass1
{
    public string AccountName { get; set; }
    public string Address1 { get; set; }
    public float MyCoolFloat { get; set; }
    public decimal DecimalHere { get; set; }
    public int SimpleWholeNumber { get; set; }

}

public sealed class TestClass1Map : ExcelClassMap<TestClass1>
{
    public TestClass1Map()
    {
        MapFromDictionary(new Dictionary<string, string>
        {
            { "Account Name", nameof(TestClass1.AccountName) },
            { "Address 1", nameof(TestClass1.Address1) },
            { "My Cool Float", nameof(TestClass1.MyCoolFloat) },
            { "A Decimal Here???", nameof(TestClass1.DecimalHere) },
            { "A simple WhoLeNumber", nameof(TestClass1.SimpleWholeNumber) },
        });
    }
}
