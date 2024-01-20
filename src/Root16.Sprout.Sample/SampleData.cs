namespace Root16.Sprout.Sample;

internal class SampleData
{
    public static IEnumerable<CreateContact> GenerateCreateContactSampleData(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            yield return new CreateContact
            {
                FirstName = $"TestFirstName{i}",
                LastName = $"TestLastName{i}"
            };
        }
    }

    public static IEnumerable<UpdateContact> GenerateUpdateContactSampleData(int amount, int startNumber)
    {
        for (int i = startNumber; i < (amount + startNumber); i++)
        {
            yield return new UpdateContact
            {
                FirstName = $"TestFirstName{i}",
                LastName = $"TestLastName{i}",
                EmailAddress = $"Test{i}@test.com"
            };
        }
    }
}
