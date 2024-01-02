namespace Root16.Sprout.Sample;

internal class SampleData
{
    public static IEnumerable<Contact> GenerateSampleData(int amount = 10)
    {
        for (int i = 0; i < amount; i++)
        {
            yield return new Contact
            {
                FirstName = $"TestFirstName{i}",
                LastName = $"TestLastName{i}"
            };
        }
    }
}
