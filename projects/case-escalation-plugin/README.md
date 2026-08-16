# Case Escalation Plugin — Dynamics 365 CE / Dataverse

A production-pattern C# plugin that automatically escalates Cases (`incident`) that have breached their SLA due date: bumps priority to High, reassigns ownership to an escalation queue, and writes an auditable escalation record.

Built to demonstrate the patterns used on a live D365 case-management implementation, reconstructed here with generic/fictional schema names (`new_sladuedate`, `new_caseescalationlog`) so it runs against any sandbox Dataverse environment with no client dependency.

## What this demonstrates

- **Plugin pipeline correctness** — registered Post-Operation, Asynchronous, on Update of `incident`, with proper `IServiceProvider` resolution.
- **Recursion guarding** — `context.Depth > 1` check, since this plugin's own `service.Update` call would otherwise re-trigger itself.
- **Re-read pattern** — the `Target` on Update only contains changed fields; the plugin re-retrieves the full record before evaluating business logic that depends on unchanged attributes.
- **Separation of concerns** — SLA evaluation (`SlaEvaluator`), queue resolution (`QueueHelper`), and audit writing (`AuditLogger`) are isolated from the plugin class so the core logic is unit-testable without mocking `IOrganizationService`.
- **No hardcoded GUIDs** — the escalation queue is resolved by name, so the same assembly works across Dev/Test/Prod without environment-specific config swaps.
- **Fail-safe audit logging** — a failure to write the audit record does not roll back the actual escalation; the two are decoupled by design.
- **Proper error surfacing** — exceptions are caught, traced, and rethrown as `InvalidPluginExecutionException` so failures are visible in the platform UI, not just in trace logs.

## Project structure

    CaseEscalation/
    ├── Plugins/
    │   └── CaseEscalationPlugin.cs   # Entry point, IPlugin implementation
    ├── Helpers/
    │   ├── SlaEvaluator.cs           # Pure logic: is this case SLA-breached?
    │   ├── QueueHelper.cs            # Resolves escalation queue by name
    │   └── AuditLogger.cs            # Writes the escalation audit record
    └── CaseEscalation.csproj

    CaseEscalation.Tests/
    ├── SlaEvaluatorTests.cs          # xUnit tests, no live environment needed
    └── CaseEscalation.Tests.csproj

## Required Dataverse schema (fictional, for demo purposes)

| Entity | Field | Type | Purpose |
|---|---|---|---|
| `incident` (Case) | `new_sladuedate` | DateTime | SLA deadline for the case |
| `new_caseescalationlog` | `new_name` | Text | Log record title |
| `new_caseescalationlog` | `new_caseid` | Lookup (incident) | Links back to the escalated case |
| `new_caseescalationlog` | `new_escalationdate` | DateTime | When escalation occurred |
| `new_caseescalationlog` | `new_reason` | Text | Why it was escalated |
| Queue named "Case Escalation Queue" | — | — | Destination owner for escalated cases |

## Registering the plugin

1. Build in Release mode; sign the assembly if your org requires strong naming.
2. Register with the Plugin Registration Tool:
   - Step: `Update` message, `incident` primary entity
   - Stage: Post-Operation
   - Execution mode: Asynchronous
   - Filtering attributes: `statuscode` (avoids firing on every unrelated field edit)
3. Ensure a queue named `Case Escalation Queue` and the `new_caseescalationlog` entity exist in the target environment (see schema table above).

## Running the tests

    dotnet test CaseEscalation.Tests/CaseEscalation.Tests.csproj

## What's intentionally out of scope

This is a portfolio demo, not a full solution package. It does not include a `.zip` solution export, a real security role assignment for the escalation queue, or a Power Automate equivalent — those are natural next additions to this repo.
