namespace NayanCareWeb.Options;

/// <summary>
/// Strongly-typed settings for the SMTP email service.
/// </summary>
public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "NayanCare Website";
    public string ToAddress { get; set; } = string.Empty;
    public bool SendConfirmationToRequester { get; set; } = true;
}
