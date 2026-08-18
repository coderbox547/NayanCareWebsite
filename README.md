# NayanCare — ASP.NET Core Website

Your `index.html` converted into an ASP.NET Core 8 app. It serves the same site
(now with a few UX/design touches) and adds a real backend for the **Request
free demo** form: submitting it calls a `POST /api/demo-request` endpoint that
emails you the lead over SMTP (and optionally sends the requester a short
confirmation email).

## Project layout

```
NayanCareWeb/
├── Program.cs                 # app startup + the /api/demo-request endpoint
├── Models/DemoRequestModel.cs # form fields + server-side validation
├── Services/SmtpEmailService.cs # sends the emails (System.Net.Mail, no extra packages)
├── Options/EmailSettings.cs   # strongly-typed SMTP config
├── appsettings.json           # SMTP settings live here
└── wwwroot/index.html         # your site (design kept, form JS now calls the API)
```

## 1. Configure SMTP

Edit `appsettings.json`:

```json
"EmailSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@gmail.com",
  "Password": "your-16-char-app-password",
  "FromAddress": "your-email@gmail.com",
  "FromName": "NayanCare Website",
  "ToAddress": "hello@nayancare.in",
  "SendConfirmationToRequester": true
}
```

**Don't commit real credentials.** For local dev, prefer user-secrets instead
of editing appsettings.json directly:

```bash
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
```

In production, set them as environment variables instead (e.g. on Azure App
Service / IIS / Docker): `EmailSettings__Username`, `EmailSettings__Password`, etc.

**Gmail note:** you can't use your normal Gmail password — enable 2-Step
Verification on the account, then create an "App Password" at
myaccount.google.com/apppasswords and use that instead. Any other SMTP
provider (Office 365, SendGrid SMTP relay, Amazon SES SMTP, your hosting
provider's SMTP) works the same way — just change `Host`/`Port`/`Username`/`Password`.

## 2. Run it

```bash
cd NayanCareWeb
dotnet restore
dotnet run
```

Then open the URL shown in the console (e.g. `https://localhost:5001`).

## 3. What happens on submit

1. The form's JS (`wwwroot/index.html`, bottom `<script>`) validates the
   required fields, then `fetch()`-POSTs JSON to `/api/demo-request`.
2. `Program.cs` validates again server-side (`DemoRequestModel.Validate()`),
   then calls `SmtpEmailService.SendDemoRequestAsync(...)`.
3. That sends an HTML email with all the form details to your `ToAddress`,
   with **Reply-To** set to the requester's email so you can reply directly.
4. If the requester gave an email and `SendConfirmationToRequester` is `true`,
   they also get a short "we've received your request" email.
5. The page shows the existing green success box on success, or a new red
   error box (with the server's message) if something fails — no more
   client-only `alert()`s.

There's a basic rate limiter (5 requests/minute) on the endpoint to reduce
spam/abuse risk on a public form; tune it in `Program.cs` if needed.

## Design touches added

- Success/error boxes now fade in, and invalid fields get a red highlight
  instead of a plain `alert()`.
- Submit button shows a small spinner and "Sending request..." while the
  email is in flight.
- Subtle shadow added to the demo form card for more depth.

Everything else — palette, typography, layout, copy — is exactly what you
already had; nothing structural was changed so this drops in cleanly.

## Run it with Docker

The project includes a multi-stage `Dockerfile` (SDK to build/publish, then
the small `aspnet` runtime image to run) plus a `docker-compose.yml` that
passes SMTP credentials in as environment variables — no secrets baked into
the image.

**Build & run directly with Docker:**

```bash
docker build -t nayancare-web .
docker run -d -p 8080:8080 \
  -e EmailSettings__Host=smtp.gmail.com \
  -e EmailSettings__Port=587 \
  -e EmailSettings__Username=your-email@gmail.com \
  -e EmailSettings__Password=your-app-password \
  -e EmailSettings__FromAddress=your-email@gmail.com \
  -e EmailSettings__ToAddress=hello@nayancare.in \
  --name nayancare-web \
  nayancare-web
```

Then open `http://localhost:8080`.

**Or with docker-compose (recommended):**

```bash
cp .env.example .env
# edit .env with your real SMTP values
docker compose up --build -d
```

Notes on the Dockerfile:
- Multi-stage build — the final image only contains the published app on the
  `aspnet` runtime base, not the full SDK, so it stays small.
- Restore is cached separately from source copy, so rebuilding after a code
  change doesn't re-download NuGet packages.
- Runs as a non-root `appuser` inside the container.
- Listens on port `8080` internally (`ASPNETCORE_URLS=http://+:8080`); map it
  to whatever host port you like with `-p`.
- Put a reverse proxy (nginx, Caddy, your cloud provider's load balancer) in
  front of it for TLS/HTTPS in production — the container itself serves plain
  HTTP on 8080.

## Going further

- Swap `SmtpEmailService` for **MailKit** (`dotnet add package MailKit`) if
  you need OAuth2 (e.g. Microsoft 365 modern auth) — `System.Net.Mail` only
  supports basic auth.
- Persist leads to a database (EF Core + SQL Server/SQLite) in addition to
  emailing them, so nothing is lost if SMTP is briefly down.
- Add a hidden honeypot field or a CAPTCHA if the public form attracts spam.
