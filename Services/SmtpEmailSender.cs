using System.Net;
using System.Net.Mail;

namespace SanalTarihcim.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toAddress, string subject, string body)
    {
        var host = _configuration["Smtp:Host"];
        var port = _configuration.GetValue<int>("Smtp:Port");
        var from = _configuration["Smtp:From"];
        var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl", true);
        var useConsoleFallback = _configuration.GetValue<bool>("Smtp:UseConsoleFallback", false);

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(from) ||
            port <= 0)
        {
            if (useConsoleFallback)
            {
                Console.WriteLine("[EMAIL DEBUG]");
                Console.WriteLine($"To: {toAddress}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine(body);
                return;
            }

            throw new InvalidOperationException("SMTP ayarları eksik. Host, From ve Port değerleri doldurulmalı.");
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(toAddress);

        await client.SendMailAsync(message);
    }
}
