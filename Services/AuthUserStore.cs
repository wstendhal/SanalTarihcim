using System.Text.Json;

namespace SanalTarihcim.Services;

public sealed class AuthUserStore
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly Dictionary<string, AuthUser> _users;

    public AuthUserStore(string contentRootPath)
    {
        var dataDirectory = Path.Combine(contentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "auth-users.json");
        _users = File.Exists(_filePath)
            ? (JsonSerializer.Deserialize<List<AuthUser>>(File.ReadAllText(_filePath)) ?? [])
                .ToDictionary(user => user.Email, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, AuthUser>(StringComparer.OrdinalIgnoreCase);
    }

    public AuthUser? Find(string email)
    {
        lock (_sync)
        {
            return _users.GetValueOrDefault(email);
        }
    }

    public bool TryAdd(AuthUser user)
    {
        lock (_sync)
        {
            if (_users.ContainsKey(user.Email))
            {
                return false;
            }

            var updatedUsers = _users.Values.Append(user).ToList();
            var temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(updatedUsers, _jsonOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);
            _users.Add(user.Email, user);
            return true;
        }
    }
}
