# Architecture

ElevateWorkforce is a **modular monolith** with a clean, dependency-inverted layer structure.

```
┌──────────────────────────────────────────────────────────────┐
│                       ElevateWorkforce.Web (MVC)                  │
│  Controllers · ViewModels · Views · wwwroot · SeedData       │
└───────────────┬──────────────────────────────────────────────┘
                │ depends on
┌───────────────▼──────────────────────────────────────────────┐
│                    ElevateWorkforce.Application                    │
│  Services (JobService, ApplicationService, UserService,      │
│  CompanyService, AIService) · DTOs · repo interfaces         │
└───────────────┬──────────────────────────────────────────────┘
                │ depends on
┌───────────────▼──────────────────────────────────────────────┐
│                    ElevateWorkforce.Infrastructure                │
│  ApplicationDbContext · EF configurations · repositories     │
│  FileStorageService · GeminiService                          │
└───────────────┬──────────────────────────────────────────────┘
                │ depends on
┌───────────────▼──────────────────────────────────────────────┐
│                       ElevateWorkforce.Domain                     │
│  Entities (Job, JobApplication, Company, User...) · Enums    │
│  Business rules (IsExpired, ProfileCompletionPercent)        │
└──────────────────────────────────────────────────────────────┘
```

- **Domain** has zero references to other projects.
- **Infrastructure** depends only on Domain and Application (repositories implement
  the Application interfaces).
- **Application** defines the seams (`IJobRepository`, `IAIService`, …) and orchestrates
  the workflow with no EF Core knowledge.
- **Web** wires everything together via `AddInfrastructure` and `AddApplication`.

## Data model (core)

- `Users` → Identity + custom `Role`, `IsActive`.
- `JobSeekers` → extended profile, resume metadata, notifications (1:1 with User).
- `Employers` → links a User to a `Company`.
- `Companies` → status workflow: `PendingVerification` → `Active` / `Rejected`.
- `Jobs` → status workflow: `PendingVerification` → `Active` / `Rejected`;
  also `Closed` / `Removed`; computed `IsExpired` from `Deadline`.
- `Applications` → tracks a JobSeeker→Job application with a `Status` + append-only
  `Timeline` (`ApplicationTimelineEntries`) plus `Notifications` to the seeker.
- `SavedJobs` → seeker bookmarking.

## Moderation / visibility rules

Public job search (`JobService.SearchAsync`) only ever returns
`Status == Active && (Deadline == null || Deadline > now)`.

Company status gates publishing: jobs can be created while a company is pending, but they
only become public after an admin approves both the company and the job.

## AI integration

`GeminiService` calls the Gemini REST API with strict-JSON prompts. `AIService`
wraps it and exposes:

- `GenerateJobDescriptionAsync` → description/responsibilities/requirements/benefits
- `GenerateCoverLetterAsync` → personalised draft from a seeker profile + job text
- `GetCandidateInsightsAsync` → match score, skills diff, strengths, gaps

When no `Gemini:ApiKey` is configured, `IsEnabled == false` and every feature returns a
"temporarily unavailable" response instead of failing. Key prompts use raw interpolated
strings (`$$"""`) to avoid JSON-brace escaping issues.

## Design-time EF & migrations

`ApplicationDbContextFactory` provides a design-time factory so `dotnet ef` works without
starting the whole host. Migrations live under
`src/ElevateHire.Infrastructure/Migrations`. On app startup, `Program.cs` runs
`db.Database.MigrateAsync()` then `SeedData.InitializeAsync`.

## Tests

- **Unit** (`ElevateWorkforce.UnitTests`) — domain rules, `JobService`, `ApplicationService`,
  `FileStorageService`. Uses an in-memory `IAsyncQueryProvider` helper to exercise EF-style
  async LINQ without a database.
- **Integration** (`ElevateWorkforce.IntegrationTests`) — WebApplicationFactory tests against a
  real PostgreSQL instance asserting page rendering, seed data, and role-based redirects.

Run integration tests only with the database container up:
`docker compose -f docker/docker-compose.yml up -d`.