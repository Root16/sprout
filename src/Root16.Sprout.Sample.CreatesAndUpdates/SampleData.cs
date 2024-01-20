using Root16.Sprout.Sample.Models;
using TaskData = Root16.Sprout.Sample.Models.TaskData;

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

    public static IEnumerable<Account> GenerateSampleAccounts(int amount = 10)
    {
        for(int i = 0;i < amount; i++)
        {
            yield return new Account()
            {
                AccountName = $"TestFirstName{i} TestLastName{i}"
            };
        }
    }

    public static IEnumerable<TaskData> GenerateSampleTasks(int amount = 10)
    {
        for (int i = 0; i < amount; i++)
        {
            yield return new TaskData()
            {
                TaskSubject = $"TestTask{i}"
            };
        }
    }

    public static IEnumerable<Letter> GenerateSampleLetters(int amount = 10)
    {
        for (int i = 0; i < amount; i++)
        {
            yield return new Letter()
            {
                LetterSubject = $"Letter{i}"
            };
        }
    }

    public static IEnumerable<Email> GenerateSampleEmails(int amount = 10)
    {
        for (int i = 0; i < amount; i++)
        {
            yield return new Email()
            {
                EmailSubject = $"Email{i}"
            };
        }
    }
}
