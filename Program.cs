using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NayanCareWeb.Models;
using NayanCareWeb.Options;
using NayanCareWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Simple in-memory rate limiter so the public form can't be used to spam SMTP.
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("demo-form", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error.html");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();
app.UseRateLimiter();

app.MapPost("/api/demo-request", async (
        [FromBody] DemoRequestModel request,
        IEmailService emailService,
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.BadRequest(new { success = false, errors });
        }

        try
        {
            await emailService.SendDemoRequestAsync(request, cancellationToken);
            return Results.Ok(new { success = true, message = "Demo request submitted successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send demo request email for {Hospital}.", request.HospitalName);
            return Results.Problem(
                detail: "We couldn't send your request right now. Please try again shortly or call us directly.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .RequireRateLimiting("demo-form");

app.Run();
