# NayanCare — Eye Hospital Management System

A web application and landing page for NayanCare, a complete Eye Hospital Management System with modules for Front Desk, Optometrist, Doctor, Pharmacy, Optical, IPD/OPD, CRM, and more.

## Architecture

- **Frontend**: Responsive landing page with interactive module showcases, pricing, and demo booking form (`wwwroot/index.html`).
- **Backend**: Node.js + Express with TypeScript (`server.ts`).
  - Serves static files and pages (`wwwroot/index.html`, `wwwroot/error.html`).
  - Rate-limited endpoint for `POST /api/demo-request` (5 requests/minute per client).
  - Validation matching the demo request model.
  - SMTP email dispatching via Nodemailer (with in-memory storage fallback when unconfigured).
  - Health check at `GET /health`.

## Environment Variables

Configurable in `.env` (refer to `.env.example`):

```bash
PORT=3000
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_ENABLE_SSL=true
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_FROM_ADDRESS=your-email@gmail.com
SMTP_FROM_NAME=NayanCare Website
SMTP_TO_ADDRESS=hello@nayancare.in
SMTP_SEND_CONFIRMATION=true
```

## Running the Application

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Typecheck and build
npm run build
```
The server runs on `http://0.0.0.0:3000`.
