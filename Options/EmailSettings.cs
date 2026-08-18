namespace NayanCareWeb.Options;

/// <summary>
/// SMTP configuration, bound from the "EmailSettings" section of appsettings.json
/// (or environment variables / user-secrets in production).
/// </summary>
public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Address the email appears to come from (usually the same as Username).</summary>
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "NayanCare Website";

    /// <summary>Where demo request notifications are delivered (your sales/support inbox).</summary>
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>If true, also sends a short confirmation email back to the requester.</summary>
    public bool SendConfirmationToRequester { get; set; } = true;
}
