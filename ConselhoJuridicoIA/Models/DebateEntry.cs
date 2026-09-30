namespace ConselhoJuridicoIA.Models;

public sealed class DebateEntry
{
    public required string Speaker { get; init; }
    public required string Role { get; init; }
    public required string Model { get; init; }
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public string Header => $"{Speaker} · {Role}";
    public string SubHeader => $"{Model} · {CreatedAt:HH:mm}";
}
