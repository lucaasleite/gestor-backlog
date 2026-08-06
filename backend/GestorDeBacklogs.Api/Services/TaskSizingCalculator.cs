namespace GestorDeBacklogs.Api.Services;

public record TaskToCreate(string Title, double Hours);

public static class TaskSizingCalculator
{
    private static readonly HashSet<int> KnownEfforts = [4, 8, 16, 24, 40];

    public static bool TryCalculate(string parentTitle, int effortHours, out IReadOnlyList<TaskToCreate> tasks)
    {
        tasks = Array.Empty<TaskToCreate>();

        if (!KnownEfforts.Contains(effortHours))
        {
            return false;
        }

        if (effortHours == 4)
        {
            tasks = new[] { new TaskToCreate(parentTitle, 4) };
            return true;
        }

        var count = effortHours / 8;
        tasks = Enumerable.Range(1, count)
            .Select(i => new TaskToCreate(count == 1 ? parentTitle : $"{parentTitle} - Parte {i}", 8))
            .ToArray();
        return true;
    }
}
