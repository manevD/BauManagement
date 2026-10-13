
using BauManagement.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

namespace BauManagement.Services.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpEmailOptions> options,
    ILogger<SmtpEmailSender> logger)
    : IEmailSender<ApplicationUser>
{
    private readonly SmtpEmailOptions _options = options.Value;

    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink)
    {
        var safeLink = WebUtility.HtmlEncode(confirmationLink);

        var body = $"""
            <!DOCTYPE html>
            <html lang="de">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
            </head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:Arial,sans-serif;color:#172033;">
                <div style="max-width:600px;margin:30px auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb;">
                    <div style="background:#0a0a0a;padding:28px;text-align:center;">
                        <h1 style="margin:0;color:#ffffff;font-size:25px;">
                            <span style="color:#ff7a00;">Bau</span>Management
                        </h1>
                    </div>

                    <div style="padding:35px 28px;text-align:center;">
                        <h2 style="margin-top:0;color:#172033;">Bitte bestätige deine E-Mail-Adresse</h2>

                        <p style="font-size:16px;line-height:1.7;color:#4b5563;">
                            Vielen Dank für deine Registrierung bei BauManagement.
                            Bitte bestätige deine E-Mail-Adresse, um dein Konto zu aktivieren.
                        </p>

                        <div style="margin:30px 0;">
                            <a href="{safeLink}"
                               style="display:inline-block;background:#ff7a00;color:#ffffff;text-decoration:none;font-weight:bold;padding:15px 28px;border-radius:8px;">
                                E-Mail-Adresse bestätigen
                            </a>
                        </div>

                        <p style="font-size:13px;line-height:1.6;color:#6b7280;">
                            Falls die Schaltfläche nicht funktioniert, kopiere diesen Link
                            in deinen Browser:
                        </p>

                        <p style="font-size:12px;line-height:1.6;word-break:break-all;">
                            <a href="{safeLink}" style="color:#ea6900;">{safeLink}</a>
                        </p>

                        <p style="font-size:13px;color:#6b7280;margin-top:25px;">
                            Wenn du dich nicht registriert hast, kannst du diese E-Mail ignorieren.
                        </p>
                    </div>

                    <div style="padding:18px;text-align:center;background:#f9fafb;color:#9ca3af;font-size:12px;">
                        © BauManagement – Webolar
                    </div>
                </div>
            </body>
            </html>
            """;

        var textBody = $"""
            Willkommen bei BauManagement!

            Bitte bestätige deine E-Mail-Adresse, indem du diesen Link öffnest:

            {confirmationLink}

            Wenn du dich nicht registriert hast, kannst du diese E-Mail ignorieren.
            """;

        return SendAsync(
            email,
            "BauManagement – E-Mail-Adresse bestätigen",
            body,
            textBody);
    }

    public Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink)
    {
        var safeLink = WebUtility.HtmlEncode(resetLink);

        var body = $"""
            <!DOCTYPE html>
            <html lang="de">
            <body style="font-family:Arial,sans-serif;padding:25px;color:#172033;">
                <h2>Passwort zurücksetzen</h2>
                <p>Du hast angefordert, dein Passwort zurückzusetzen.</p>
                <p>
                    <a href="{safeLink}"
                       style="display:inline-block;background:#ff7a00;color:#ffffff;text-decoration:none;padding:13px 22px;border-radius:7px;font-weight:bold;">
                        Passwort zurücksetzen
                    </a>
                </p>
                <p>Falls du diese Anfrage nicht gestellt hast, kannst du diese E-Mail ignorieren.</p>
            </body>
            </html>
            """;

        var textBody = $"""
            Du hast angefordert, dein Passwort zurückzusetzen.

            Öffne diesen Link:
            {resetLink}

            Falls du diese Anfrage nicht gestellt hast, kannst du diese E-Mail ignorieren.
            """;

        return SendAsync(
            email,
            "BauManagement – Passwort zurücksetzen",
            body,
            textBody);
    }

    public Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode)
    {
        var safeCode = WebUtility.HtmlEncode(resetCode);

        var body = $"""
            <!DOCTYPE html>
            <html lang="de">
            <body style="font-family:Arial,sans-serif;padding:25px;color:#172033;">
                <h2>Passwort zurücksetzen</h2>
                <p>Dein Bestätigungscode lautet:</p>
                <p style="font-size:24px;font-weight:bold;letter-spacing:3px;">{safeCode}</p>
            </body>
            </html>
            """;

        var textBody = $"Dein Code zum Zurücksetzen des Passworts lautet: {resetCode}";

        return SendAsync(
            email,
            "BauManagement – Passwort zurücksetzen",
            body,
            textBody);
    }

    private async Task SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        string textBody)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            _options.FromName,
            _options.FromEmail));

        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.SslOnConnect);

            await client.AuthenticateAsync(
                _options.Username,
                _options.Password);

            await client.SendAsync(message);

            logger.LogInformation(
                "E-Mail erfolgreich an {Recipient} versendet. Betreff: {Subject}",
                recipient,
                subject);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "E-Mail-Versand über SMTP an {Recipient} fehlgeschlagen.",
                recipient);

            throw;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true);
        }
    }
}
