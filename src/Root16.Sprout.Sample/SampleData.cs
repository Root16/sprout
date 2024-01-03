using Root16.Sprout.Sample.Models;

namespace Root16.Sprout.Sample;

internal class SampleData
{
    public static IEnumerable<Contact> GenerateSampleContacts(int amount = 10)
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

    public static IEnumerable<Account> GenerateSampleAccounts(int amount = 10)
    {
        for(int i = 0;i < amount; i++)
        {
            yield return new Account()
            {
                AccountName = $"TestFirstName{i} TestLastName {i}"
            };
        }
    }
}
