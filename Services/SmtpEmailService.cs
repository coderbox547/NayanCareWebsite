using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using NayanCareWeb.Models;
using NayanCareWeb.Options;

namespace NayanCareWeb.Services;

/// <summary>
/// Sends the "New demo request" notification (and optionally a confirmation
/// back to the requester) over SMTP using the built-in System.Net.Mail client.
/// No extra NuGet packages required. Configure credentials in appsettings.json,
/// user-secrets, or environment variables (EmailSettings__Username, etc.).
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendDemoRequestAsync(DemoRequestModel request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.ToAddress))
        {
            throw new InvalidOperationException(
                "Email settings are not configured. Set EmailSettings:Host and EmailSettings:ToAddress " +
                "in appsettings.json (or user-secrets) before sending demo requests.");
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        // 1) Notify the sales/support inbox.
        using (var notification = BuildNotificationMessage(request))
        {
            await client.SendMailAsync(notification, cancellationToken);
        }

        // 2) Optionally send a short confirmation back to the requester.
        if (_settings.SendConfirmationToRequester && !string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                using var confirmation = BuildConfirmationMessage(request);
                await client.SendMailAsync(confirmation, cancellationToken);
            }
            catch (Exception ex)
            {
                // Don't fail the whole request just because the courtesy confirmation email failed.
                _logger.LogWarning(ex, "Demo request notification sent, but the confirmation email to {Email} failed.", request.Email);
            }
        }

        _logger.LogInformation("Demo request email sent for {Hospital} ({City}).", request.HospitalName, request.City);
    }

    private MailMessage BuildNotificationMessage(DemoRequestModel r)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = $"New Demo Request – {r.HospitalName} ({r.City})",
            Body = BuildNotificationHtml(r),
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(_settings.ToAddress));

        if (!string.IsNullOrWhiteSpace(r.Email))
        {
            message.ReplyToList.Add(new MailAddress(r.Email!, r.Name));
        }

        return message;
    }

    private MailMessage BuildConfirmationMessage(DemoRequestModel r)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = "We've received your NayanCare demo request",
            Body = BuildConfirmationHtml(r),
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(r.Email!, r.Name));
        return message;
    }

    private static string Encode(string? value) => WebUtilityHtmlEncode(value);

    private static string WebUtilityHtmlEncode(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : System.Net.WebUtility.HtmlEncode(value);

    private static string Row(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return $"<tr><td style=\"padding:6px 14px 6px 0;color:#6a8a7a;font-size:13px;white-space:nowrap;\">{Encode(label)}</td>" +
               $"<td style=\"padding:6px 0;color:#162d3a;font-size:14px;font-weight:500;\">{Encode(value)}</td></tr>";
    }

    private static string BuildNotificationHtml(DemoRequestModel r)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;\">");
        sb.Append("<h2 style=\"color:#162d3a;\">New free demo request</h2>");
        sb.Append("<table cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;\">");
        sb.Append(Row("Name", r.Name));
        sb.Append(Row("Designation", r.Designation));
        sb.Append(Row("Hospital", r.HospitalName));
        sb.Append(Row("City", r.City));
        sb.Append(Row("State", r.State));
        sb.Append(Row("Phone", r.Phone));
        sb.Append(Row("Email", r.Email));
        sb.Append(Row("Number of doctors", r.DoctorsRange));
        sb.Append(Row("Hospital size", r.HospitalSize));
        sb.Append(Row("Primary interest", r.ModuleInterest));
        sb.Append("</table>");
        if (!string.IsNullOrWhiteSpace(r.Notes))
        {
            sb.Append("<p style=\"color:#6a8a7a;font-size:13px;margin-top:14px;\">Notes:</p>");
            sb.Append($"<p style=\"color:#162d3a;font-size:14px;white-space:pre-wrap;\">{Encode(r.Notes)}</p>");
        }
        sb.Append("<p style=\"color:#9ca9a3;font-size:12px;margin-top:24px;\">Submitted from the NayanCare website demo form.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildConfirmationHtml(DemoRequestModel r)
    {
        return $@"
        <div style=""font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:0 auto;"">
          <h2 style=""color:#162d3a;"">Thanks, {Encode(r.Name)}!</h2>
          <p style=""color:#3a5a4a;font-size:14px;line-height:1.6;"">
            We've received your request for a free NayanCare demo on behalf of
            <strong>{Encode(r.HospitalName)}</strong>. Our team will call you at
            <strong>{Encode(r.Phone)}</strong> within 24 hours to schedule a personalized
            30-minute walkthrough.
          </p>
          <p style=""color:#6a8a7a;font-size:13px;"">— The NayanCare team</p>
        </div>";
    }
}
