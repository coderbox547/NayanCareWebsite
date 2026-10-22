using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NayanCareWeb.Models;
using NayanCareWeb.Options;
using NayanCareWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// In-memory rate limiter: 5 requests per 1 minute
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
app.UseDefaultFiles(); // serves wwwroot/index.html at "/"
app.UseStaticFiles();  // serves files from wwwroot
app.UseRateLimiter();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    app = "NayanCareWeb (.NET 8)",
    timestamp = DateTime.UtcNow
}));

// POST /api/demo-request
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
