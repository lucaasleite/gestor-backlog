namespace GestorDeBacklogs.Api.Services;

// Regra combinada com o usuário: um item sem a tag de planejamento é considerado Fora da Sprint,
// independente de ter ou não uma tag explícita de "fora da sprint" - a classificação é sempre binária.
public static class SprintCategoryClassifier
{
    public static bool IsPlanned(IReadOnlyList<string>? tags, string plannedTagName)
    {
        if (tags is null || tags.Count == 0)
        {
            return false;
        }

        return tags.Any(t => string.Equals(t.Trim(), plannedTagName.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
