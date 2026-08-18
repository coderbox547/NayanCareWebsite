using System.ComponentModel.DataAnnotations;

namespace NayanCareWeb.Models;

/// <summary>
/// Matches the fields collected by the "Request your free demo" form in wwwroot/index.html.
/// </summary>
public class DemoRequestModel
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Designation { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string HospitalName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string City { get; set; } = string.Empty;

    [StringLength(80)]
    public string? State { get; set; }

    [Required, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [StringLength(40)]
    public string? DoctorsRange { get; set; }

    [StringLength(60)]
    public string? HospitalSize { get; set; }

    [StringLength(80)]
    public string? ModuleInterest { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Server-side validation beyond what [Required]/[EmailAddress] catch on their own,
    /// returned as a list of human-readable messages.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Please enter your name.");
        if (string.IsNullOrWhiteSpace(Designation)) errors.Add("Please enter your designation.");
        if (string.IsNullOrWhiteSpace(HospitalName)) errors.Add("Please enter your hospital name.");
        if (string.IsNullOrWhiteSpace(City)) errors.Add("Please enter your city.");
        if (string.IsNullOrWhiteSpace(Phone)) errors.Add("Please enter your mobile number.");

        if (!string.IsNullOrWhiteSpace(Email))
        {
            var attr = new EmailAddressAttribute();
            if (!attr.IsValid(Email)) errors.Add("Please enter a valid email address.");
        }

        return errors;
    }
}
