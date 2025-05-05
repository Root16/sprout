using CsvHelper.Configuration;

namespace Root16.Sprout.CSV.Sample.Models;

public class TestClass1
{
    public int ID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Gender { get; set; }
    public string IpAddress { get; set; }
}

public sealed class TestClass1Map : ClassMap<TestClass1>
{
    public TestClass1Map()
    {
        Map(m => m.ID).Name("id");
        Map(m => m.FirstName).Name("first_name");
        Map(m => m.LastName).Name("last_name");
        Map(m => m.Email).Name("email");
        Map(m => m.Gender).Name("gender");
        Map(m => m.IpAddress).Name("ip_address");
    }
}