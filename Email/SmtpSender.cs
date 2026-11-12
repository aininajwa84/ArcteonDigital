using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Dewi.Email;

public sealed class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string ContactRecipient { get; set; } = "";
    public string CareersRecipient { get; set; } = "";
}

public sealed class SmtpSender(IOptions<SmtpSettings> options, ILogger<SmtpSender> logger)
{
    public async Task<bool> SendAsync(string subject, MailboxAddress replyTo, BodyBuilder body,
        bool career, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var recipient = career ? settings.CareersRecipient : settings.ContactRecipient;
        if (string.IsNullOrWhiteSpace(settings.Host) || settings.Port is < 1 or > 65535 ||
            settings.Security is not (SecureSocketOptions.StartTls or SecureSocketOptions.SslOnConnect) ||
            string.IsNullOrWhiteSpace(settings.Username) != string.IsNullOrEmpty(settings.Password) ||
            !MailboxAddress.TryParse(settings.FromAddress, out var from) ||
            !MailboxAddress.TryParse(recipient, out var to))
        {
            logger.LogWarning("SMTP configuration is incomplete or invalid.");
            return false;
        }

        using var message = new MimeMessage { Subject = subject, Body = body.ToMessageBody() };
        message.From.Add(from);
        message.To.Add(to);
        message.ReplyTo.Add(replyTo);

        using var client = new SmtpClient();
        var stage = "connection";
        try
        {
            using (var connectionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                connectionTimeout.CancelAfter(TimeSpan.FromSeconds(20));
                await client.ConnectAsync(settings.Host, settings.Port, settings.Security, connectionTimeout.Token);
                if (!string.IsNullOrWhiteSpace(settings.Username))
                {
                    stage = "authentication";
                    await client.AuthenticateAsync(settings.Username, settings.Password, connectionTimeout.Token);
                }
            }

            stage = "message submission";
            using var submissionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            submissionTimeout.CancelAfter(TimeSpan.FromSeconds(90));
            await client.SendAsync(message, submissionTimeout.Token);
            // Acceptance of DATA completes the submission. Closing the connection must not trigger a retry.
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("SMTP {Stage} timed out.", stage);
            return false;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
            ex is IOException or System.Net.Sockets.SocketException or SmtpCommandException or
                MailKit.Security.AuthenticationException or System.Security.Authentication.AuthenticationException or
                SslHandshakeException)
        {
            logger.LogWarning("SMTP {Stage} failed ({ErrorType}, status {SmtpStatus}).",
                stage, ex.GetType().Name, (ex as SmtpCommandException)?.StatusCode);
            return false;
        }
    }
}
