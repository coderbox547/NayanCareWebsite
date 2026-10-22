# NayanCare — Eye Hospital Management System

A complete Eye Hospital Management System with modules for Front Desk, Optometrist, Doctor, Pharmacy, Optical, IPD/OPD, CRM, and more.

## Architecture & Frameworks

The application is fully structured as an **ASP.NET Core 8** web application, with all modern ophthalmic features:

### Project Structure (ASP.NET Core 8)

```
NayanCareWeb/
├── NayanCareWeb.csproj        # ASP.NET Core 8 project file
├── NayanCareWeb.sln           # Visual Studio solution file
├── Program.cs                 # App startup, middleware, and /api/demo-request minimal API
├── Models/
│   └── DemoRequestModel.cs    # Form fields with SelectedPlan, doctor counts, module interests, and validation
├── Options/
│   └── EmailSettings.cs       # Strongly-typed SMTP configuration
├── Services/
│   ├── IEmailService.cs       # Email service interface
│   └── SmtpEmailService.cs    # Lead notification & confirmation emails with HTML templating
├── appsettings.json           # Application settings & SMTP configuration
├── appsettings.Development.json
├── Dockerfile                 # Multi-stage .NET 8 SDK & ASP.NET runtime image
└── wwwroot/
    ├── index.html             # High-impact landing page, interactive Clinical Hub, pricing & FAQs
    └── error.html             # Friendly error fallback page
```

---

## Running with ASP.NET Core 8 (.NET CLI / Visual Studio)

### 1. Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 2. Configure SMTP (Optional)
Edit `appsettings.json` or set environment variables:

```json
"EmailSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@gmail.com",
  "Password": "your-app-password",
  "FromAddress": "your-email@gmail.com",
  "FromName": "NayanCare Website",
  "ToAddress": "hello@nayancare.in",
  "SendConfirmationToRequester": true
}
```

### 3. Run the application

```bash
dotnet restore
dotnet run
```
Then navigate to `http://localhost:5000` or `https://localhost:5001`.

---

## Running with Docker

```bash
docker build -t nayancare-web .
docker run -d -p 8080:8080 --name nayancare-web nayancare-web
```
Access the application at `http://localhost:8080`.

---

## Running in Node.js / AI Studio Preview

The project also contains an Express / TypeScript development server (`server.ts`) for browser environments:

```bash
npm install
npm run dev
```
Listens on `http://0.0.0.0:3000`.
