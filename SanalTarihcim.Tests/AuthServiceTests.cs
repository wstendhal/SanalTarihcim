using SanalTarihcim.Services;
using Xunit;

namespace SanalTarihcim.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldSendVerificationCode_AndLoginWithEmailAndPassword()
    {
        var emailSender = new FakeEmailSender();
        var authService = new AuthService(emailSender);

        var registerResult = await authService.RegisterAsync("Ayşe", "Yılmaz", "ayse@test.com", "12345678");

        Assert.True(registerResult.Success);

        var verificationCode = authService.GetPendingLoginCode("ayse@test.com");
        Assert.False(string.IsNullOrWhiteSpace(verificationCode));

        var loginResult = authService.Login("ayse@test.com", "12345678");
        Assert.True(loginResult.Success);

        var invalidLoginResult = authService.Login("ayse@test.com", "yanlisSifre");
        Assert.False(invalidLoginResult.Success);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string toAddress, string subject, string body)
        {
            return Task.CompletedTask;
        }
    }
}
