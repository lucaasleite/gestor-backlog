namespace GestorDeBacklogs.Api.Services;

// Fallback usado quando o work item não tem o campo de tamanho preenchido: procura uma tag
// #PP/#P/#M/#G/#GG (System.Tags) e usa o mapeamento abaixo pra também inferir o Effort.
public static class SizeTagResolver
{
    private static readonly Dictionary<string, int> HoursByTag = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PP"] = 4,
        ["P"] = 8,
        ["M"] = 16,
        ["G"] = 24,
        ["GG"] = 40,
    };

    public static (string? SizeLabel, int? EffortHours) Resolve(string? currentSizeLabel, int? currentEffortHours, string? tags)
    {
        if (!string.IsNullOrWhiteSpace(currentSizeLabel) || string.IsNullOrWhiteSpace(tags))
        {
            return (currentSizeLabel, currentEffortHours);
        }

        foreach (var rawTag in tags.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var tag = rawTag.Trim().TrimStart('#');
            if (HoursByTag.TryGetValue(tag, out var hours))
            {
                return (tag.ToUpperInvariant(), currentEffortHours ?? hours);
            }
        }

        return (currentSizeLabel, currentEffortHours);
    }
}
