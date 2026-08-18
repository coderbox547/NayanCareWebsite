using NayanCareWeb.Models;

namespace NayanCareWeb.Services;

public interface IEmailService
{
    Task SendDemoRequestAsync(DemoRequestModel request, CancellationToken cancellationToken = default);
}
