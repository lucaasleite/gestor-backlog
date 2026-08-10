namespace GestorDeBacklogs.Api.Services;

// Fallback usado quando o campo de Effort não está preenchido: tenta inferir as horas a partir
// do próprio campo de tamanho (Custom.EstimativaSize já com "PP"/"P"/"M"/"G"/"GG") ou, se este
// também estiver vazio, de uma tag #PP/#P/#M/#G/#GG (System.Tags).
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
        if (currentEffortHours is not null)
        {
            return (currentSizeLabel, currentEffortHours);
        }

        if (!string.IsNullOrWhiteSpace(currentSizeLabel) &&
            HoursByTag.TryGetValue(currentSizeLabel.Trim().TrimStart('#'), out var hoursFromLabel))
        {
            return (currentSizeLabel, hoursFromLabel);
        }

        if (string.IsNullOrWhiteSpace(tags))
        {
            return (currentSizeLabel, currentEffortHours);
        }

        foreach (var rawTag in tags.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var tag = rawTag.Trim().TrimStart('#');
            if (HoursByTag.TryGetValue(tag, out var hours))
            {
                return (currentSizeLabel ?? tag.ToUpperInvariant(), hours);
            }
        }

        return (currentSizeLabel, currentEffortHours);
    }
}
