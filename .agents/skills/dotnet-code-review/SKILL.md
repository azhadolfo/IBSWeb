---
name: dotnet-code-review
description: Review ASP.NET Core and C# changes in IBSWeb for correctness, data integrity, security, breaking changes, EF Core risks, performance, and maintainability.
---

# .NET Code Review

Use this skill when the user asks to review C# or ASP.NET Core code, inspect Git changes, review a pull request, find possible bugs, or assess whether a change is ready to commit. This skill is for review only: do not modify application code unless the user separately asks for an implementation.

## Repository context

Follow the repository's `AGENTS.md` and existing patterns. IBSWeb is a layered ASP.NET Core MVC business and accounting system:

- `IBSWeb/` contains the web app, controllers, areas, Razor views, middleware, and hubs.
- `IBS.Services/` contains business logic, middleware, scheduling, and integrations.
- `IBS.DataAccess/` contains `ApplicationDbContext`, EF Core configuration, repositories, and migrations.
- `IBS.Models/`, `IBS.DTOs/`, and `IBS.Utility/` contain domain models, transport models, and shared utilities.

Preserve business and accounting boundaries. Do not treat an apparent difference as a defect when repository context or explicit user clarification shows that the behavior is intentional.

## Review workflow

1. Establish the review scope and confirmed base. Inspect the working tree and Git changes first (`git status`, then the appropriate diff or commit/PR ref). Do not review unrelated or pre-existing code as a new finding.
2. Read the surrounding implementation, callers, models, repositories, migrations, and relevant views or configuration. Trace data from input through validation, authorization, persistence, and output.
3. Prioritize findings in this order: correctness and incorrect behavior; data integrity and accounting impact; security; breaking changes; EF Core/database behavior; performance; maintainability; style.
4. For each candidate issue, verify the concrete execution path and explain the affected scenario. Avoid speculative findings, cleanup-only comments, and overengineering.
5. Report only actionable findings, followed by positive observations and a final assessment.

## Severity

Use one of these severity levels:

- **Critical** — exploitable security issue, corruption or loss of important data, or a failure that can broadly prevent core operation.
- **High** — material production regression, incorrect accounting/business result, authorization bypass, or likely outage in an important workflow.
- **Medium** — real user-visible defect, integrity risk with limited scope, or significant performance/reliability issue.
- **Low** — limited-risk correctness or maintainability issue with a practical improvement.

Do not assign severity based only on code smell. State the impact and conditions that make the issue reachable.

## ASP.NET Core and C# checks

Check, when relevant:

- Dependency injection registrations, service lifetimes, scoped `DbContext` usage, and accidental service locator patterns.
- Async/await end-to-end for I/O; blocking `.Result`, `.Wait()`, synchronous database or file calls, and missing cancellation where the surrounding API supports it.
- Model binding, nullability, validation, boundary checks, and trust of client-supplied identifiers or amounts.
- Authentication and authorization on actions, areas, APIs, scheduled endpoints, file access, and SignalR flows.
- Exception handling, transaction boundaries, logging, and whether failures leave partial state or leak sensitive details.
- Correct HTTP results, redirects, route values, view models, and Razor rendering/encoding.
- Concurrency, duplicate submissions, idempotency, and resource disposal when the workflow can be retried or run in parallel.

## EF Core and database checks

Check, when relevant:

- N+1 queries, accidental client-side evaluation, missing `AsNoTracking()` for read-only queries, and inefficient projections.
- `Include`/`ThenInclude` correctness, filtered relationships, one-to-many joins that duplicate rows or amounts, and null/empty result handling.
- Query filters, ordering, pagination, stable page boundaries, and whether counts and totals use the same filter as the displayed rows.
- `SaveChanges`/`SaveChangesAsync` placement, multiple saves, transaction scope, rollback behavior, and whether related entities are persisted consistently.
- Race conditions around uniqueness, inventory, balances, approvals, journals, and other accounting-sensitive updates.
- Migration safety, PostgreSQL compatibility, defaults, nullability, indexes, foreign keys, and destructive schema changes.

## Review output

Return:

1. **Findings**, ordered by severity. Each finding must include severity, file and line (or the closest precise location), the concrete problem, why it occurs, and a concise remediation direction.
2. **Positive observations**, limited to meaningful safeguards or improvements visible in the reviewed changes.
3. **Final assessment**, stating whether the changes are valid as reviewed, need fixes before merge, or cannot be fully verified, and why.

If there are no actionable findings, say so explicitly. Distinguish build/test verification from static review; a failed or unavailable build does not prove a defect, and a successful build does not prove behavior is correct.
