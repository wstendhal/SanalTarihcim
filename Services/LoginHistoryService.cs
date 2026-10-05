using System.Text.Json;

namespace SanalTarihcim.Services;

public sealed record LoginHistoryEntry(
    string FirstName,
    string LastName,
    string Email,
    DateTimeOffset LoggedInAtUtc);

public sealed class LoginHistoryService
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly List<LoginHistoryEntry> _entries;

    public LoginHistoryService(string contentRootPath)
    {
        var dataDirectory = Path.Combine(contentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "login-history.json");
        _entries = File.Exists(_filePath)
            ? JsonSerializer.Deserialize<List<LoginHistoryEntry>>(File.ReadAllText(_filePath)) ?? []
            : [];
    }

    public IReadOnlyList<LoginHistoryEntry> GetAll()
    {
        lock (_sync)
        {
            return _entries.ToList();
        }
    }

    public void RecordSuccessfulLogin(AuthUser user)
    {
        lock (_sync)
        {
            var entry = new LoginHistoryEntry(
                user.FirstName,
                user.LastName,
                user.Email,
                DateTimeOffset.UtcNow);
            var updatedEntries = new List<LoginHistoryEntry>(_entries.Count + 1) { entry };
            updatedEntries.AddRange(_entries);
            var temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(updatedEntries, _jsonOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);
            _entries.Insert(0, entry);
        }
    }
}
