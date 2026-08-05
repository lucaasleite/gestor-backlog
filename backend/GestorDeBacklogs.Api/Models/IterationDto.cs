namespace GestorDeBacklogs.Api.Models;

public record IterationDto(string Id, string Name, string Path, bool IsCurrent, DateTime? FinishDate = null);
