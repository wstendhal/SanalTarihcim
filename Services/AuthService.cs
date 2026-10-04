using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace SanalTarihcim.Services;

public sealed class AuthUser
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public sealed record AuthResult(bool Success, string Message, string? VerificationCode = null);

public sealed class AuthService
{
    private readonly IEmailSender _emailSender;
    private readonly ConcurrentDictionary<string, AuthUser> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _loginCodes = new(StringComparer.OrdinalIgnoreCase);

    public AuthService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        var trimmedFirstName = (firstName ?? string.Empty).Trim();
        var trimmedLastName = (lastName ?? string.Empty).Trim();
        var trimmedPassword = password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedFirstName) ||
            string.IsNullOrWhiteSpace(trimmedLastName) ||
            string.IsNullOrWhiteSpace(normalizedEmail) ||
            string.IsNullOrWhiteSpace(trimmedPassword))
        {
            return new AuthResult(false, "Tüm alanlar zorunludur.");
        }

        if (!normalizedEmail.Contains('@'))
        {
            return new AuthResult(false, "Geçerli bir e-posta adresi giriniz.");
        }

        if (_users.ContainsKey(normalizedEmail))
        {
            return new AuthResult(false, "Bu e-posta adresi zaten kayıtlı.");
        }

        var user = new AuthUser
        {
            Email = normalizedEmail,
            FirstName = trimmedFirstName,
            LastName = trimmedLastName,
            PasswordHash = ComputeHash(trimmedPassword)
        };

        _users[normalizedEmail] = user;

        var loginCode = GenerateCode();
        _loginCodes[normalizedEmail] = loginCode;

        try
        {
            await _emailSender.SendEmailAsync(
                normalizedEmail,
                "Sanal Tarihçim - Doğrulama Kodu",
                $"<p>Merhaba {trimmedFirstName},</p>" +
                "<p>Arşiv erişimi için doğrulama kodunuz aşağıdadır:</p>" +
                $"<h2>{loginCode}</h2>" +
                "<p>Bu kod kısa süre içinde geçerli olacaktır.</p>");

            return new AuthResult(true, "Kayıt başarılı. Doğrulama kodu e-posta adresinize gönderildi.", loginCode);
        }
        catch (Exception)
        {
            return new AuthResult(true, "Kayıt başarılı. E-posta gönderilemedi; doğrulama kodu aşağıda gösterilmiştir.", loginCode);
        }
    }

    public AuthResult Login(string email, string password)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        var trimmedPassword = password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(trimmedPassword))
        {
            return new AuthResult(false, "E-posta ve şifre zorunludur.");
        }

        if (!_users.TryGetValue(normalizedEmail, out var user))
        {
            return new AuthResult(false, "Bu e-posta adresi kayıtlı değil.");
        }

        var passwordHash = ComputeHash(trimmedPassword);
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            return new AuthResult(false, "Şifre yanlış.");
        }

        return new AuthResult(true, "Giriş başarılı.");
    }

    public bool VerifyRegistrationCode(string email, string code)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        var normalizedCode = (code ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(normalizedCode))
        {
            return false;
        }

        if (!_loginCodes.TryGetValue(normalizedEmail, out var expectedCode))
        {
            return false;
        }

        var isValid = string.Equals(normalizedCode, expectedCode, StringComparison.OrdinalIgnoreCase);

        if (isValid)
        {
            _loginCodes.TryRemove(normalizedEmail, out _);
        }

        return isValid;
    }

    public bool TryValidateLogin(string email, string code)
    {
        return VerifyRegistrationCode(email, code);
    }

    public string? GetPendingLoginCode(string email)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        return _loginCodes.TryGetValue(normalizedEmail, out var code) ? code : null;
    }

    private static string GenerateCode()
    {
        var random = RandomNumberGenerator.GetInt32(100000, 999999);
        return random.ToString();
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
