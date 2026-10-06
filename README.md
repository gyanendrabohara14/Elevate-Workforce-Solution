# ElevateWorkforce

An **AI-assisted job recruitment platform built for Nepal**. ASP.NET Core MVC plus a Web API, PostgreSQL, Identity, JWT, Redis, SMTP, and Gemini API.

Employers post jobs, job seekers build profiles and apply, and administrators moderate the verification workflow. Gemini-powered features generate job descriptions, AI-draft cover letters, and candidate matching insights.

The responsive Razor interface includes separate job-seeker and employer workflows, a WebGL cloud-sky background with a solid-color fallback, and shared brand logos and favicon.

## Features

- **Roles**: Admin · Employer · Job Seeker (ASP.NET Core Identity)
- **Job Seekers**
  - Browse/search jobs with filters (keyword, location, category, type, experience, salary, remote, posted date)
  - Saved jobs, application tracking with full status timeline
  - Profile completion scoring, resume upload (PDF/DOC/DOCX), notifications
  - AI-generated cover-letter drafts
- **Employers**
  - Company profile (verified by an admin before jobs go live)
  - Post / edit / remove jobs, view applicants per job
  - Move applications through the pipeline (Under Review → Shortlisted → Interview → Selected/Rejected)
  - AI candidate insights: match score, matching/missing skills, strengths, gaps
- **Admin**
  - Platform KPIs and moderation queues (pending companies and jobs)
  - Approve/reject/unpublish/restore jobs, verify/reject companies, disable users
  - All rows are real database queries
- **Web experience**: Responsive Razor views, shared branding, and an animated WebGL cloud background that respects reduced-motion preferences
- **API registration**
  - `POST /api/User` registers JobSeeker and Employer accounts
  - Returns validation, duplicate-email, role, and Identity errors with appropriate HTTP status codes
  - Issues a JWT and stores the registration response in Redis
  - Sends a welcome email through the configured SMTP server
- **API documentation**
  - OpenAPI JSON is available at `/openapi/v1.json` in Development

## Tech stack

| Layer | Technology |
| --- | --- |
| App | ASP.NET Core MVC + Web API (.NET 10), Razor views, CSS, JavaScript, and WebGL |
| Data | PostgreSQL 16, EF Core 10, Entity Framework migrations |
| Auth | ASP.NET Core Identity (users, custom roles, cookies) + JWT bearer API authentication |
| Cache | Redis via `IDistributedCache` with an in-memory fallback |
| Email | MailKit SMTP service; smtp4dev is included for local development |
| AI | Google Gemini REST API (graceful fallback when no key) |
| Storage | Local file storage for resumes and company logos |
| Tests | xUnit, Moq, WebApplicationFactory |

## Project layout

```
ElevateWorkforce.slnx
docker/docker-compose.yml      # App + PostgreSQL + Redis + smtp4dev stack
Dockerfile                     # Container build for the ASP.NET Core app
src/
  ElevateWorkforce.Domain           # Entities, enums, business rules
  ElevateWorkforce.Application      # Services, DTOs, interfaces
  ElevateWorkforce.Infrastructure   # EF Core, repositories, storage, Redis, email, Gemini
  ElevateWorkforce.Web              # MVC/API controllers, views, wwwroot assets
tests/
  ElevateWorkforce.UnitTests        # unit tests (services, domain, storage, API registration)
  ElevateWorkforce.IntegrationTests # real web server + DB integration tests
```

## Prerequisites

- .NET SDK 10.x
- Docker Desktop (for PostgreSQL, Redis, and smtp4dev)

## Getting started

1. Start the full local stack with Docker:

   ```bash
   docker compose -f docker/docker-compose.yml up -d --build
   ```

   This starts the ASP.NET app on `http://localhost:8080`, PostgreSQL on `localhost:5435`, Redis on `localhost:6379`, and smtp4dev on SMTP port `2525` with its inbox at http://localhost:5000.

2. If you want to run the app directly on your machine instead of Docker, create/update the schema and start the app:

   ```bash
   dotnet ef database update --project src/ElevateWorkforce.Infrastructure --startup-project src/ElevateWorkforce.Web
   dotnet run --project src/ElevateWorkforce.Web
   ```

   Open http://localhost:5266

   The API is available at http://localhost:5266/api/User and the OpenAPI
   document is available at http://localhost:5266/openapi/v1.json.

   > Run with `dotnet run --project` (not the raw DLL) so CSS/JS/static assets are
   > served correctly via static web assets in Development.

3. Run the tests (requires the database container from step 1 for integration tests):

   ```bash
   dotnet test
   ```

## Seed accounts

The app seeds realistic demo data (users, companies, jobs, applications) on first run.

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@elevateworkforce.local` | `Admin@123` |
| Employer | `employer@elevateworkforce.local` | `Employer@123` |
| Job seeker | `jobseeker@elevateworkforce.local` | `Jobseeker@123` |

## Configuration

`src/ElevateWorkforce.Web/appsettings.json`:

- `ConnectionStrings:DefaultConnection` — PostgreSQL connection
- `ConnectionStrings:Redis` — Redis connection, normally `localhost:6379`
- `Jwt:Key` — signing key; use a secret with at least 32 characters outside local development
- `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes` — JWT validation and lifetime settings
- `Email:Host`, `Email:Port`, `Email:FromEmail`, `Email:FromName`, `Email:UseSsl` — SMTP settings
- `Gemini:ApiKey` — Google Gemini API key; keep this blank in the committed file and store the real key in User Secrets
- `Gemini:Endpoint` — REST endpoint (default `https://generativelanguage.googleapis.com/v1beta`)
- `Gemini:Model` — model name (default `gemini-3.6-flash`)
- `FileStorage:RootPath` — where resumes/logos are stored (default `wwwroot/uploads`)

### Enable Gemini locally

Gemini has a limited free tier for eligible models and accounts, but it is not unlimited. Free requests are subject to changing rate and daily quotas; billing may be required for higher limits or models without free access. Check the current Google AI Studio pricing and quota page before relying on it in production.

Create a key in Google AI Studio, then store it locally without editing `appsettings.json`:

```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY" --project src/ElevateWorkforce.Web
```

The application already calls the Gemini REST API for job descriptions, cover letters, and candidate insights. If no key is configured, those features return a graceful unavailable response instead of failing the website.

## Verification workflow

1. Employer registers and creates a company profile → **PendingVerification**.
2. Admin approves the company → **Active**. Until then no jobs go live.
3. Employer posts a job → **PendingVerification**.
4. Admin approves → jobs become public (only `Active` and non-expired jobs are ever visible).
5. Job seekers apply; employers pipeline candidates; AI assists with matching.

## Deployment

This is an ASP.NET Core server application and cannot be deployed directly as a static site on Netlify. Deploy the root `Dockerfile` to a container-capable host such as Render, Railway, or Azure App Service.

Configure these production environment variables on the hosting provider:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection` — managed PostgreSQL connection string
- `ConnectionStrings__Redis` — managed Redis connection string, or omit it to use the in-memory fallback
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes`
- `Email__Host`, `Email__Port`, `Email__FromEmail`, `Email__FromName`, `Email__UseSsl`
- `Gemini__ApiKey`, `Gemini__Endpoint`, `Gemini__Model`

The service listens on port `8080` inside the container. Do not use the local Docker Compose ports or development passwords in production.

