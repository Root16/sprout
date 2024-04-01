using Root16.Sprout.Sample.ParallelSteps.Models;
using TaskData = Root16.Sprout.Sample.ParallelSteps.Models.TaskData;

namespace Root16.Sprout.Sample.ParallelSteps;

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
        for (int i = 0; i < amount; i++)
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
