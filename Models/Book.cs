namespace SanalTarihcim.Models;

public sealed record Book(
    Guid Id,
    string Title,
    string Author,
    string Description,
    string ImageUrl,
    string? DetailUrl);