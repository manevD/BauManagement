namespace BauManagement.Services.Email;

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = "smtp.ionos.de";
    public int Port { get; set; } = 465;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "info@webolar.de";
    public string FromName { get; set; } = "BauManagement";
}