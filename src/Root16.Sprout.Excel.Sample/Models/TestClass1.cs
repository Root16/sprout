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
        MapFromDictionary(new Dictionary<string, List<string>>
        {
            { nameof(TestClass1.AccountName),  ["Account Name", "Account Name 1"] },
            { nameof(TestClass1.Address1), ["Address 1", "Address 2"] },
            { nameof(TestClass1.MyCoolFloat), ["My Cool Float", "My Cool Float 1"] },
            { nameof(TestClass1.DecimalHere), ["A Decimal Here???", "A Decimal Here??? 1"] },
            { nameof(TestClass1.SimpleWholeNumber), ["A simple WhoLeNumber", "A simple WhoLeNumber 1"] },
        });
    }
}