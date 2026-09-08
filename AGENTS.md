# AGENTS.md

## Purpose

This file defines how a coding agent should work in this repository.

This repo is a layered ASP.NET Core MVC business and accounting system. It uses Razor Views, area-based controllers, EF Core with PostgreSQL, session-aware middleware patterns, Serilog, SignalR, and cloud storage integrations.

The agent should make the smallest clean change that matches the existing codebase and keeps the app easy to maintain.

## Working Defaults

* Prefer simple, explicit code over flexible abstractions.
* Prefer small focused edits over broad rewrites.
* Follow existing repository patterns unless they are clearly harmful.
* Keep controllers thin and put business logic in services.
* Keep data access in EF Core and repository classes already used by the project.
* Avoid introducing new packages unless the platform or current dependencies cannot solve the problem well.
* Do not add comments that restate obvious code.

## Repository Shape

Use the current solution structure as the default:

* `IBSWeb/` for the ASP.NET Core web app, `Program.cs`, SignalR hubs, Razor views, and area-based MVC controllers
* `IBSWeb/Areas/` for module-specific MVC entry points and views such as `Admin`, `User`, `Bienes`, `Filpride`, and `Identity`
* `IBS.Services/` for business logic, middleware, scheduling, and external integrations
* `IBS.DataAccess/` for `ApplicationDbContext`, EF Core configuration, repositories, and migrations
* `IBS.Models/` for domain models and entity types
* `IBS.DTOs/` for DTOs and request/response transport models
* `IBS.Utility/` for shared helpers and cross-cutting utilities

Place changes in the project that already owns the behavior instead of collapsing code back into the web project.

## Architectural Rules

* Keep business rules out of Razor views and controllers.
* Respect existing project boundaries between web, services, data access, models, DTOs, and utility code.
* Put validation close to the request boundary or service boundary where it is easiest to find.
* Use interfaces where the repo already benefits from them, especially for service seams and external integrations.
* Do not create interfaces for every class by default.
* Do not add pass-through service layers with no behavior.
* Do not add generic repository wrappers over EF Core.
* Prefer concrete, readable flows over clever generic helpers.

## Coding Style

* Read the applicable `.editorconfig` rules before editing a file. `.editorconfig` is the source of truth for formatting, naming, and C# style; it takes precedence over style guidance in this file or surrounding code.
* Follow the effective rules for the file, including file-pattern sections and any nested `.editorconfig` overrides. Do not duplicate their settings here, so future configuration changes remain authoritative.
* Apply configured style preferences to new and changed code, including preferences with suggestion or silent severity. Severity controls diagnostics, not whether the preference applies.
* Use existing conventions and the simplicity guidelines below where `.editorconfig` does not specify a preference.
* Use clear names and shallow control flow.
* Prefer guard clauses and early returns.
* Keep methods focused.
* Preserve nullable correctness.
* Use async end-to-end for I/O-bound work.
* Do not block async code with `.Result`, `.Wait()`, or similar patterns.
* Follow `.editorconfig` for namespace declarations, braces, constructors, expression bodies, imports, and other syntax choices.
* Keep one public type per file unless a small local grouping is clearly simpler.

## MVC and HTTP Guidance

* This repo is controller-and-view based. Default to extending existing controllers and Razor views.
* Prefer the existing `Areas` structure and keep new endpoints, views, and related code in the appropriate module.
* Keep controller actions focused on request handling, authorization decisions, model binding, and choosing the response.
* Put non-trivial query logic, storage workflows, and access rules into services or existing repository classes.
* Preserve existing route patterns unless the task requires a route change.
* Return consistent HTTP results and avoid inventing custom response wrappers unless the repo already expects them.

## Data and Persistence

* Prefer EF Core patterns already present in the repo.
* Reuse the existing repository and unit-of-work patterns already present in `IBS.DataAccess` where the surrounding code depends on them.
* Keep queries explicit and easy to reason about.
* Add migrations only when schema changes are required for the task.
* Keep each migration focused on one logical schema change.
* Make destructive schema changes explicit.
* Respect PostgreSQL behavior already configured by the app.

## Security and Operations

* Validate and sanitize external input.
* Do not log secrets, tokens, credentials, or connection strings.
* Preserve authentication, authorization, maintenance-mode behavior, scheduled job endpoints, and SignalR flows unless the task requires changing them.
* Be careful with file handling and cloud storage paths. Prefer existing workflow services over duplicating storage logic.
* Preserve audit, accounting, and approval-related flows unless the task explicitly changes them.

## Dependencies

* Prefer the .NET base class library and existing Microsoft packages first.
* Reuse existing packages already in the repo when reasonable.
* Add a third-party package only with clear justification.
* Remove unused code or dependencies only when directly relevant to the task.

## Tests and Verification

There is currently no separate test project in this repository. When changing behavior:

* Add automated tests if a test project exists or if the task includes creating one.
* If no test project exists, do not fabricate a large test harness just to satisfy process.
* At minimum, run the relevant build or targeted verification commands when possible.

Default validation commands:

* `dotnet build "Integrated Business System.sln"`
* `dotnet test` only if a test project exists

If a schema change is made, also verify the relevant EF Core migration artifacts.

## Change Discipline

When making changes, follow this order:

1. Understand the requirement.
2. Inspect the existing code path and conventions.
3. Make the smallest clean change that solves the problem.
4. Update tests when practical and appropriate.
5. Run relevant validation.
6. Avoid unrelated refactors.

Before finishing, check changed code against the applicable `.editorconfig` rules, including encoding, line endings, indentation, and final newlines. Keep formatting changes limited to the files and code involved in the task; do not reformat the repository solely to fix pre-existing style differences.

## Avoid By Default

Do not introduce these unless clearly justified by the task:

* Generic repository abstractions over EF Core
* Marker interfaces
* Deep inheritance hierarchies
* Static global mutable state
* Reflection-heavy or dynamic designs
* Placeholder abstractions for future requirements
* Broad folder reorganizations
* Cosmetic-only churn

## Final Rule

Prefer the solution that is easiest for the next maintainer to read, test, and change while staying aligned with this repository's current MVC, EF Core, and layered solution structure.
