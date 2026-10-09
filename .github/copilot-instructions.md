# Copilot instructions for BarberMenagment

## Project snapshot

- This is an ASP.NET Core MVC web application in C#.
- `BarberMenagment.csproj` targets `net10.0`, enables nullable reference types and implicit usings, and is configured for Linux Docker tooling.
- The current application is the MVC template shell: `Program.cs` registers MVC, configures HTTPS redirection/routing/authorization, maps static assets, and uses the conventional `{controller=Home}/{action=Index}/{id?}` route.
- Controllers return Razor views from `Views/<ControllerName>/<ActionName>.cshtml`. Shared layout, imports, validation scripts, Bootstrap, jQuery, and site assets are under `Views/` and `wwwroot/`.
- `docker-compose.yaml` currently provides only PostgreSQL 16 on port `5432`, using database `barbershop_db` and the development credentials defined in that file. `Data/ApplicationDbContext.cs` is registered in `Program.cs` and uses the `DefaultConnection` configuration entry.

## Build, run, and test

Run commands from the repository root:

```bash
dotnet restore
dotnet build
dotnet run --launch-profile https
```

The launch profiles use `https://localhost:7073` and `http://localhost:5099`. Use `dotnet run --launch-profile http` when HTTPS is not needed.

For local database setup and schema changes:

```bash
docker compose up -d postgres
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
dotnet ef database update
```

The development PostgreSQL connection is configured in `appsettings.Development.json`. Keep production connection strings in environment variables or deployment configuration.

There is currently no test project or lint configuration in the repository. `dotnet test` therefore has no project tests to execute. When a test project is added, run the full suite with `dotnet test`; run one test or a focused group with:

```bash
dotnet test <path-to-test-project.csproj> --filter "FullyQualifiedName~Namespace.Type.TestName"
```

No separate JavaScript build is present; browser assets are checked in under `wwwroot/lib` and application JavaScript/CSS is in `wwwroot/js` and `wwwroot/css`.

## Architecture and request flow

- `Program.cs` is the composition root and HTTP pipeline. Register cross-cutting services there and preserve middleware ordering: exception handling/HSTS for non-development, HTTPS redirection, routing, authorization, then endpoint/static-asset mapping.
- `Controllers/` contains MVC endpoints. Keep actions focused on request orchestration and view/model selection; put domain operations in services once the barbershop features are introduced.
- `Models/` contains view/domain models. Use dedicated view models for non-trivial form or page shapes rather than placing calculations and persistence logic in Razor views.
- `Views/Shared/_Layout.cshtml` supplies the site shell and loads Bootstrap, jQuery, and application assets. Page-specific scripts belong in a view `Scripts` section.
- `wwwroot/` is the static web root. Reuse the checked-in Bootstrap 5 and jQuery assets instead of adding a second frontend dependency path.
- `docker-compose.yaml` is local infrastructure only. Keep connection settings/environment-specific values out of source-controlled production configuration.

## Repository-specific conventions

- Use standard C# naming and nullable-safe code. Existing namespaces follow `BarberMenagment.<Area>` and file-scoped namespaces are used in C# files.
- Use ASP.NET Core Tag Helpers (`asp-controller`, `asp-action`, `asp-for`, and related helpers) for MVC links and forms.
- Use Bootstrap 5 classes for responsive layouts, forms, tables, and modals; keep Razor markup simple and move complex presentation calculations into view models or helpers.
- The intended product UI language is Serbian; keep code identifiers and internal APIs in clear standard C# English unless an existing domain term is specifically Serbian.
- If persistence is added, use PostgreSQL with EF Core code-first migrations. Prefer `IEntityTypeConfiguration<T>` classes for entity mapping, PostgreSQL-compatible timestamp types, UTC date values, integer identity keys, and correctly nullable optional foreign keys.
- Authentication uses cookie authentication plus session state, not JWT or the ASP.NET Identity system. Public registration creates only `Client` users; `Barber` accounts are inserted manually in the database. Passwords use `PasswordHasher<User>`. Store the user identifier and role (`Client` or `Barber`) in session/claims and protect barber-only endpoints with role authorization or the established session-check mechanism.
- Appointment slots must remain derived from barber shifts and filtered against active appointments (`Status == "Aktivno"`); do not introduce a hardcoded slot table.
- Barber-canceled appointments require a `RazlogOtkazivanja` value and should notify the client through the MailKit SMTP service asynchronously.
- Transactional appointment confirmation emails use `IEmailService`/MailKit and `Smtp` configuration (`Host`, `Port`, `UseSsl`, `Username`, `Password`, `FromEmail`, `FromName`). Keep SMTP credentials in environment variables or local, untracked configuration.

## Configuration and generated files

- Use `.env` for local overrides and secrets, based on the tracked `.env.example`; the application loads it before building configuration. Environment variable names use ASP.NET Core's `__` section separator, such as `ConnectionStrings__DefaultConnection` and `Smtp__Password`. Existing system environment variables take precedence over `.env` values. Do not commit `.env`, credentials, or connection strings containing real secrets.
- Treat `bin/`, `obj/`, IDE metadata, and other generated artifacts as build output; do not edit them as source.
- Keep the canonical Copilot guidance in `.github/copilot-instructions.md`. The repository also contains a legacy hidden file named `.github/.copilot-instructions.md`; update it only when intentionally keeping legacy guidance synchronized.
