using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SanalTarihcim.Services;
using Xunit;

namespace SanalTarihcim.Tests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), $"SanalTarihcimTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task RegisterAsync_CreatesUserOnlyAfterEmailCodeIsVerified_AndRecordsSuccessfulLogin()
    {
        var emailSender = new FakeEmailSender();
        var history = new LoginHistoryService(_testDirectory);
        var userStore = new AuthUserStore(_testDirectory);
        var authService = CreateAuthService(emailSender, history, userStore);

        var registration = await authService.RegisterAsync("Ayşe", "Yılmaz", "ayse@test.com", "12345678");

        Assert.True(registration.Success);
        Assert.DoesNotContain("123456", registration.Message);
        Assert.Equal("ayse@test.com", emailSender.LastRecipient);

        var codeMatch = Regex.Match(emailSender.LastBody!, @"<h2>(\d{6})</h2>");
        Assert.True(codeMatch.Success);
        var code = codeMatch.Groups[1].Value;
        var incorrectCode = code == "000000" ? "000001" : "000000";

        Assert.False(authService.Login("ayse@test.com", "12345678").Success);
        Assert.False(await authService.VerifyRegistrationCodeAsync("ayse@test.com", incorrectCode));
        Assert.True(await authService.VerifyRegistrationCodeAsync("ayse@test.com", code));
        Assert.False(await authService.VerifyRegistrationCodeAsync("ayse@test.com", code));

        Assert.True(authService.Login("ayse@test.com", "12345678").Success);
        Assert.False(authService.Login("ayse@test.com", "yanlisSifre").Success);

        var restartedAuthService = CreateAuthService(
            emailSender,
            new LoginHistoryService(_testDirectory),
            new AuthUserStore(_testDirectory));
        Assert.True(restartedAuthService.Login("ayse@test.com", "12345678").Success);

        var entry = Assert.Single(history.GetAll());
        Assert.Equal("Ayşe", entry.FirstName);
        Assert.Equal("Yılmaz", entry.LastName);
        Assert.Equal("ayse@test.com", entry.Email);
    }

    [Fact]
    public async Task RegisterAsync_DoesNotCreateAccountWhenEmailCannotBeSent()
    {
        var emailSender = new FakeEmailSender { Failure = new SmtpException("SMTP unavailable") };
        var authService = CreateAuthService(
            emailSender,
            new LoginHistoryService(_testDirectory),
            new AuthUserStore(_testDirectory));

        var registration = await authService.RegisterAsync("Ayşe", "Yılmaz", "ayse@test.com", "12345678");

        Assert.False(registration.Success);
        Assert.False(authService.Login("ayse@test.com", "12345678").Success);
    }

    [Fact]
    public async Task RegisterAsync_RejectsPasswordsShorterThanEightCharacters()
    {
        var emailSender = new FakeEmailSender();
        var authService = CreateAuthService(
            emailSender,
            new LoginHistoryService(_testDirectory),
            new AuthUserStore(_testDirectory));

        var registration = await authService.RegisterAsync("Ayşe", "Yılmaz", "ayse@test.com", "1234567");

        Assert.False(registration.Success);
        Assert.Null(emailSender.LastRecipient);
    }

    [Fact]
    public async Task RegisterAsync_ResendsANewCodeAndInvalidatesThePreviousCode()
    {
        var emailSender = new FakeEmailSender();
        var authService = CreateAuthService(
            emailSender,
            new LoginHistoryService(_testDirectory),
            new AuthUserStore(_testDirectory));

        await authService.RegisterAsync("Ayşe", "Yılmaz", "ayse@test.com", "12345678");
        var firstCode = Regex.Match(emailSender.LastBody!, @"<h2>(\d{6})</h2>").Groups[1].Value;

        await authService.RegisterAsync("Ayşe", "Yılmaz", "ayse@test.com", "12345678");
        var secondCode = Regex.Match(emailSender.LastBody!, @"<h2>(\d{6})</h2>").Groups[1].Value;

        Assert.NotEqual(firstCode, secondCode);
        Assert.False(await authService.VerifyRegistrationCodeAsync("ayse@test.com", firstCode));
        Assert.True(await authService.VerifyRegistrationCodeAsync("ayse@test.com", secondCode));
    }

    [Fact]
    public async Task VerifyRegistrationCodeAsync_LocksRegistrationAfterFiveIncorrectAttempts()
    {
        var emailSender = new FakeEmailSender();
        var authService = CreateAuthService(
            emailSender,
            new LoginHistoryService(_testDirectory),
            new AuthUserStore(_testDirectory));
        await authService.RegisterAsync("Ayşe", "Yılmaz", "ayse@test.com", "12345678");
        var actualCode = Regex.Match(emailSender.LastBody!, @"<h2>(\d{6})</h2>").Groups[1].Value;
        var incorrectCode = actualCode == "000000" ? "000001" : "000000";

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.False(await authService.VerifyRegistrationCodeAsync("ayse@test.com", incorrectCode));
        }

        Assert.False(await authService.VerifyRegistrationCodeAsync("ayse@test.com", actualCode));
        Assert.False(authService.Login("ayse@test.com", "12345678").Success);
    }

    [Fact]
    public void LoginHistoryService_PersistsEntriesAcrossInstances()
    {
        var history = new LoginHistoryService(_testDirectory);
        history.RecordSuccessfulLogin(new AuthUser
        {
            FirstName = "Ayşe",
            LastName = "Yılmaz",
            Email = "ayse@test.com"
        });

        var persistedHistory = new LoginHistoryService(_testDirectory);

        var entry = Assert.Single(persistedHistory.GetAll());
        Assert.Equal("Ayşe", entry.FirstName);
        Assert.Equal("Yılmaz", entry.LastName);
        Assert.Equal("ayse@test.com", entry.Email);
    }

    [Fact]
    public async Task SmtpEmailSender_RequiresCredentialsBeforeSending()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:Host"] = "smtp.gmail.com",
                ["Smtp:Port"] = "587",
                ["Smtp:From"] = "sender@gmail.com"
            })
            .Build();
        var emailSender = new SmtpEmailSender(configuration);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => emailSender.SendEmailAsync("recipient@example.com", "Test", "Test"));

        Assert.Contains("Username ve Password", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private static AuthService CreateAuthService(
        FakeEmailSender emailSender,
        LoginHistoryService history,
        AuthUserStore userStore) =>
        new(emailSender, userStore, history, NullLogger<AuthService>.Instance);

    private sealed class FakeEmailSender : IEmailSender
    {
        public string? LastRecipient { get; private set; }
        public string? LastBody { get; private set; }
        public Exception? Failure { get; init; }

        public Task SendEmailAsync(string toAddress, string subject, string body)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            LastRecipient = toAddress;
            LastBody = body;
            return Task.CompletedTask;
        }
    }
}
