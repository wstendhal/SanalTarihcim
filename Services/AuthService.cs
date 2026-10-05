using System.Collections.Concurrent;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace SanalTarihcim.Services;

public sealed class AuthUser
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public sealed record AuthResult(bool Success, string Message);

public sealed class AuthService(
    IEmailSender emailSender,
    AuthUserStore userStore,
    LoginHistoryService loginHistory,
    ILogger<AuthService> logger)
{
    private static readonly TimeSpan VerificationCodeLifetime = TimeSpan.FromMinutes(10);
    private const int MaximumVerificationAttempts = 5;
    private const int MinimumPasswordLength = 8;

    private readonly PasswordHasher<AuthUser> _passwordHasher = new();
    private readonly SemaphoreSlim _registrationLock = new(1, 1);
    private readonly ConcurrentDictionary<string, PendingRegistration> _pendingRegistrations =
        new(StringComparer.OrdinalIgnoreCase);

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

        if (trimmedPassword.Length < MinimumPasswordLength)
        {
            return new AuthResult(false, "Şifre en az 8 karakter olmalıdır.");
        }

        try
        {
            if (!string.Equals(
                    new MailAddress(normalizedEmail).Address,
                    normalizedEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResult(false, "Geçerli bir e-posta adresi giriniz.");
            }
        }
        catch (FormatException)
        {
            return new AuthResult(false, "Geçerli bir e-posta adresi giriniz.");
        }

        await _registrationLock.WaitAsync();
        try
        {
            if (userStore.Find(normalizedEmail) is not null)
            {
                return new AuthResult(false, "Bu e-posta adresi zaten kayıtlı.");
            }

            var previousCode = _pendingRegistrations.TryGetValue(normalizedEmail, out var previous)
                ? previous.Code
                : null;
            var code = GenerateCode(previousCode);
            var user = new AuthUser
            {
                Email = normalizedEmail,
                FirstName = trimmedFirstName,
                LastName = trimmedLastName
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, trimmedPassword);

            var pendingRegistration = new PendingRegistration(
                user,
                code,
                DateTimeOffset.UtcNow.Add(VerificationCodeLifetime));

            try
            {
                await emailSender.SendEmailAsync(
                    normalizedEmail,
                    "Sanal Tarihçim - E-posta Doğrulama Kodu",
                    $"<p>Merhaba {HtmlEncoder.Default.Encode(trimmedFirstName)},</p>" +
                    "<p>Hesabınızı oluşturmak için aşağıdaki 6 haneli doğrulama kodunu kullanın:</p>" +
                    $"<h2>{code}</h2>" +
                    "<p>Bu kod 10 dakika içinde geçerliliğini yitirir.</p>");
            }
            catch (Exception exception) when (exception is SmtpException or InvalidOperationException)
            {
                _pendingRegistrations.TryRemove(normalizedEmail, out _);
                logger.LogError(exception, "Kayıt doğrulama e-postası gönderilemedi.");
                return new AuthResult(false, "Doğrulama e-postası gönderilemedi. Lütfen daha sonra tekrar deneyin.");
            }

            _pendingRegistrations[normalizedEmail] = pendingRegistration;
            return new AuthResult(true, "Doğrulama kodu e-posta adresinize gönderildi.");
        }
        finally
        {
            _registrationLock.Release();
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

        var user = userStore.Find(normalizedEmail);
        if (user is null)
        {
            return new AuthResult(false, "Bu e-posta adresi kayıtlı değil.");
        }

        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, trimmedPassword) ==
            PasswordVerificationResult.Failed)
        {
            return new AuthResult(false, "Şifre yanlış.");
        }

        loginHistory.RecordSuccessfulLogin(user);
        return new AuthResult(true, "Giriş başarılı.");
    }

    public async Task<bool> VerifyRegistrationCodeAsync(string email, string code)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        var normalizedCode = (code ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(normalizedCode))
        {
            return false;
        }

        await _registrationLock.WaitAsync();
        try
        {
            if (!_pendingRegistrations.TryGetValue(normalizedEmail, out var pending))
            {
                return false;
            }

            if (pending.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _pendingRegistrations.TryRemove(normalizedEmail, out _);
                return false;
            }

            if (normalizedCode.Length != 6 ||
                normalizedCode.Any(character => character is < '0' or > '9') ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(normalizedCode),
                    Encoding.ASCII.GetBytes(pending.Code)))
            {
                if (Interlocked.Increment(ref pending.FailedAttempts) >= MaximumVerificationAttempts)
                {
                    _pendingRegistrations.TryRemove(normalizedEmail, out _);
                }

                return false;
            }

            _pendingRegistrations.TryRemove(normalizedEmail, out _);
            return userStore.TryAdd(pending.User);
        }
        finally
        {
            _registrationLock.Release();
        }
    }

    private static string GenerateCode(string? previousCode)
    {
        string code;
        do
        {
            code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }
        while (code == previousCode);

        return code;
    }

    private sealed class PendingRegistration(AuthUser user, string code, DateTimeOffset expiresAtUtc)
    {
        public AuthUser User { get; } = user;
        public string Code { get; } = code;
        public DateTimeOffset ExpiresAtUtc { get; } = expiresAtUtc;
        public int FailedAttempts;
    }
}
