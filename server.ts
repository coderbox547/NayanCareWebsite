import express, { Request, Response, NextFunction } from 'express';
import cors from 'cors';
import path from 'path';
import { fileURLToPath } from 'url';
import nodemailer from 'nodemailer';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = parseInt(process.env.PORT || '3000', 10);
const HOST = '0.0.0.0';

// In-memory rate limiting: 5 requests per 60 seconds per IP
const rateLimitMap = new Map<string, { count: number; resetTime: number }>();
const RATE_LIMIT_WINDOW_MS = 60 * 1000;
const MAX_REQUESTS_PER_WINDOW = 5;

function demoRateLimiter(req: Request, res: Response, next: NextFunction) {
  const ip = (req.headers['x-forwarded-for'] as string) || req.socket.remoteAddress || 'unknown';
  const now = Date.now();
  const clientData = rateLimitMap.get(ip);

  if (!clientData || now > clientData.resetTime) {
    rateLimitMap.set(ip, { count: 1, resetTime: now + RATE_LIMIT_WINDOW_MS });
    return next();
  }

  if (clientData.count >= MAX_REQUESTS_PER_WINDOW) {
    return res.status(429).json({
      detail: 'Too many demo requests submitted from this network. Please wait a minute before trying again.'
    });
  }

  clientData.count += 1;
  next();
}

// In-memory submissions store
interface DemoSubmission {
  name: string;
  designation: string;
  hospitalName: string;
  city: string;
  state?: string;
  phone: string;
  email?: string;
  doctorsRange?: string;
  hospitalSize?: string;
  moduleInterest?: string;
  selectedPlan?: string;
  notes?: string;
  submittedAt: string;
}

const submissions: DemoSubmission[] = [];

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Serve static assets from wwwroot
const wwwrootDir = path.join(__dirname, 'wwwroot');
app.use(express.static(wwwrootDir));

// Health check endpoint
app.get('/health', (_req: Request, res: Response) => {
  res.json({ status: 'healthy', app: 'NayanCare', timestamp: new Date().toISOString() });
});

// Helper for escaping HTML in email body
function escapeHtml(text?: string): string {
  if (!text) return '';
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function buildNotificationHtml(r: DemoSubmission): string {
  const row = (label: string, value?: string) => {
    if (!value) return '';
    return `<tr><td style="padding:6px 14px 6px 0;color:#6a8a7a;font-size:13px;white-space:nowrap;">${escapeHtml(label)}</td><td style="padding:6px 0;color:#162d3a;font-size:14px;font-weight:500;">${escapeHtml(value)}</td></tr>`;
  };

  return `
    <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;">
      <h2 style="color:#162d3a;">New free demo request</h2>
      <table cellpadding="0" cellspacing="0" style="border-collapse:collapse;width:100%;">
        ${row('Name', r.name)}
        ${row('Designation', r.designation)}
        ${row('Hospital', r.hospitalName)}
        ${row('City', r.city)}
        ${row('State', r.state)}
        ${row('Phone', r.phone)}
        ${row('Email', r.email)}
        ${row('Number of doctors', r.doctorsRange)}
        ${row('Hospital size', r.hospitalSize)}
        ${row('Interested Plan', r.selectedPlan)}
        ${row('Primary interest', r.moduleInterest)}
      </table>
      ${r.notes ? `
        <p style="color:#6a8a7a;font-size:13px;margin-top:14px;">Notes:</p>
        <p style="color:#162d3a;font-size:14px;white-space:pre-wrap;">${escapeHtml(r.notes)}</p>
      ` : ''}
      <p style="color:#9ca9a3;font-size:12px;margin-top:24px;">Submitted from the NayanCare website demo form.</p>
    </div>
  `;
}

function buildConfirmationHtml(r: DemoSubmission): string {
  return `
    <div style="font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:0 auto;">
      <h2 style="color:#162d3a;">Thanks, ${escapeHtml(r.name)}!</h2>
      <p style="color:#3a5a4a;font-size:14px;line-height:1.6;">
        We've received your request for a free NayanCare demo on behalf of
        <strong>${escapeHtml(r.hospitalName)}</strong>. Our team will call you at
        <strong>${escapeHtml(r.phone)}</strong> within 24 hours to schedule a personalized
        30-minute walkthrough.
      </p>
      <p style="color:#6a8a7a;font-size:13px;">— The NayanCare team</p>
    </div>
  `;
}

// POST /api/demo-request
app.post('/api/demo-request', demoRateLimiter, async (req: Request, res: Response) => {
  const {
    name,
    designation,
    hospitalName,
    city,
    state,
    phone,
    email,
    doctorsRange,
    hospitalSize,
    moduleInterest,
    selectedPlan,
    notes
  } = req.body || {};

  // Server-side validation matching DemoRequestModel.cs
  const errors: string[] = [];

  if (!name || typeof name !== 'string' || !name.trim()) errors.push('Please enter your name.');
  if (!designation || typeof designation !== 'string' || !designation.trim()) errors.push('Please enter your designation.');
  if (!hospitalName || typeof hospitalName !== 'string' || !hospitalName.trim()) errors.push('Please enter your hospital name.');
  if (!city || typeof city !== 'string' || !city.trim()) errors.push('Please enter your city.');
  if (!phone || typeof phone !== 'string' || !phone.trim()) errors.push('Please enter your mobile number.');

  if (email && typeof email === 'string' && email.trim()) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email.trim())) {
      errors.push('Please enter a valid email address.');
    }
  }

  if (errors.length > 0) {
    return res.status(400).json({ success: false, errors });
  }

  const submission: DemoSubmission = {
    name: name.trim().slice(0, 120),
    designation: designation.trim().slice(0, 120),
    hospitalName: hospitalName.trim().slice(0, 160),
    city: city.trim().slice(0, 80),
    state: state ? String(state).trim().slice(0, 80) : undefined,
    phone: phone.trim().slice(0, 20),
    email: email && String(email).trim() ? String(email).trim().slice(0, 160) : undefined,
    doctorsRange: doctorsRange ? String(doctorsRange).trim().slice(0, 40) : undefined,
    hospitalSize: hospitalSize ? String(hospitalSize).trim().slice(0, 60) : undefined,
    moduleInterest: moduleInterest ? String(moduleInterest).trim().slice(0, 80) : undefined,
    selectedPlan: selectedPlan ? String(selectedPlan).trim().slice(0, 80) : undefined,
    notes: notes ? String(notes).trim().slice(0, 2000) : undefined,
    submittedAt: new Date().toISOString()
  };

  submissions.push(submission);
  console.log(`[Demo Request] Received from ${submission.name} for ${submission.hospitalName} (${submission.city})`);

  // Email delivery using SMTP if configured
  const smtpHost = process.env.SMTP_HOST || "smtp.gmail.com";
  const smtpPort = parseInt(process.env.SMTP_PORT || '587', 10);
  const smtpUser = process.env.SMTP_USERNAME || "bhanu.chauhan83@gmail.com";
  const smtpPass = process.env.SMTP_PASSWORD || "utjc mdxz lefg shnn";
  const fromAddress = process.env.SMTP_FROM_ADDRESS || smtpUser || 'no-reply@nayancare.in';
  const fromName = process.env.SMTP_FROM_NAME || 'NayanCare Website';
  const toAddress = process.env.SMTP_TO_ADDRESS || "bhanu_chauhan83@yahoo.com";
  const sendConfirmation = process.env.SMTP_SEND_CONFIRMATION === 'true' || process.env.SMTP_SEND_CONFIRMATION === '1';

  if (smtpHost && toAddress) {
    try {
      const transporter = nodemailer.createTransport({
        host: smtpHost,
        port: smtpPort,
        secure: smtpPort === 465 || process.env.SMTP_ENABLE_SSL === 'true',
        auth: smtpUser && smtpPass ? { user: smtpUser, pass: smtpPass } : undefined
      });

      // 1. Send notification to hospital sales/support inbox
      await transporter.sendMail({
        from: `"${fromName}" <${fromAddress}>`,
        to: toAddress,
        replyTo: submission.email ? `"${submission.name}" <${submission.email}>` : undefined,
        subject: `New Demo Request – ${submission.hospitalName} (${submission.city})`,
        html: buildNotificationHtml(submission)
      });

      // 2. Optionally send confirmation to requester
      if (sendConfirmation && submission.email) {
        try {
          await transporter.sendMail({
            from: `"${fromName}" <${fromAddress}>`,
            to: submission.email,
            subject: "We've received your NayanCare demo request",
            html: buildConfirmationHtml(submission)
          });
        } catch (confErr) {
          console.warn('[Demo Request] Notification sent, but confirmation email failed:', confErr);
        }
      }
    } catch (mailErr) {
      console.error('[Demo Request] Failed to deliver email via SMTP:', mailErr);
      // In production with explicit SMTP, return error if desired; or return failure message
      if (process.env.NODE_ENV === 'production') {
        return res.status(500).json({
          detail: "We couldn't send your request right now. Please try again shortly or call us directly."
        });
      }
    }
  } else {
    console.info('[Demo Request] SMTP settings not fully configured (SMTP_HOST/SMTP_TO_ADDRESS). Stored submission in-memory.');
  }

  return res.status(200).json({
    success: true,
    message: 'Demo request submitted successfully.'
  });
});

// Fallback to index.html for root or SPA paths
app.get('*', (req: Request, res: Response) => {
  res.sendFile(path.join(wwwrootDir, 'index.html'));
});

// Global error handler
app.use((err: Error, _req: Request, res: Response, _next: NextFunction) => {
  console.error('[Error]', err);
  res.status(500).sendFile(path.join(wwwrootDir, 'error.html'));
});

// Start listening on 0.0.0.0:3000
app.listen(PORT, HOST, () => {
  console.log(`NayanCare Web server running at http://${HOST}:${PORT}`);
});
