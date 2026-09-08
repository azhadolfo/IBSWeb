# MSAP → IBSWeb.MMSI Migration Plan

## Problem

Two sibling repos (`IBSWeb.MMSI` and `MSAP/MMSI.IBS`) share the same IBS framework but have diverged. We need to bring MSAP's maritime/billing modules into IBSWeb.MMSI **without breaking the sync relationship** with the upstream fork.

## Core Principle

**Extend, never replace.** Every shared infrastructure file (`BaseEntity`, `Company`, `ApplicationDbContext`, `UnitOfWork`, `IUnitOfWork`, `Repository`, `MemoryCacheService`, `MaintenanceMiddleware`, `Enums`) stays untouched or only receives additive changes. All MSAP business logic lives under its own namespace boundary.

---

## Phase 1: Namespace Isolation (zero conflict risk)

Move all MSAP-specific code into the `Msap` namespace prefix. This is the single most important step — it prevents merge conflicts with shared files.

### 1.1 Models (`IBS.Models`)

| Current (MSAP) | Target (MMSI) | Action |
|----------------|---------------|--------|
| `IBS.Models.Msap.*` | `IBS.Models.Msap.*` | **Keep as-is** — already namespaced |
| `IBS.Models.MasterFile.Employee` | `IBS.Models.Msap.MasterFile.Employee` | Move under `Msap/` subfolder |
| `IBS.Models.MasterFile.BankAccount` | Keep as-is if schema matches, else `IBS.Models.Msap.MasterFile.BankAccount` | Compare schemas first |
| `IBS.Models.Books.*` | `IBS.Models.Msap.Books.*` | Move under `Msap/` if not shared |

**Already correctly namespaced (no action):**
- `IBS.Models.Msap.Billing`, `Collection`, `DispatchTicket`, `JobOrder`, `VesselSchedule`, `TariffRate`, `CollectionBill`
- `IBS.Models.Msap.MasterFile.*` (Vessel, Port, Terminal, Principal, Tugboat, TugboatOwner, TugMaster, Service, UserAccess)
- `IBS.Models.Msap.ViewModels.*`

### 1.2 Repositories (`IBS.DataAccess`)

| Current (MSAP) | Target (MMSI) | Action |
|----------------|---------------|--------|
| `Repository/Msap/*` | `Repository/Msap/*` | **Keep as-is** — already namespaced |
| `Repository/MasterFile/*` (Employee, BankAccount, etc.) | `Repository/Msap/MasterFile/*` | Move MSAP-specific repos under `Msap/` |

**Already correctly namespaced (no action):**
- `IBS.DataAccess.Repository.Msap.BillingRepository`
- `IBS.DataAccess.Repository.Msap.CollectionRepository`
- All 20 repos under `Repository/Msap/`

### 1.3 Services (`IBS.Services`)

| Current (MSAP) | Target (MMSI) | Action |
|----------------|---------------|--------|
| `IBS.Services.JobOrderService` | `IBS.Services.Msap.JobOrderService` | Move to `Msap/` subfolder |
| `IBS.Services.BillingService` | `IBS.Services.Msap.BillingService` | Move to `Msap/` subfolder |
| ... all 22 MSAP services | `IBS.Services.Msap.*` | Move to `Msap/` subfolder |

### 1.4 Controllers (`IBSWeb/Areas`)

| Current (MSAP) | Target (MMSI) | Action |
|----------------|---------------|--------|
| `Areas/User/Controllers/*` | `Areas/Msap/Controllers/*` | **New area** — cleanest separation |
| `Areas/Admin/Controllers/PostedPeriodController` | `Areas/Msap/Controllers/PostedPeriodController` or `Areas/Admin/Controllers/PostedPeriodController` | Depends on if PostedPeriod is shared |
| `Areas/SuperAdmin/*` | Skip or merge into existing Admin area | Low priority |

**Create `Areas/Msap/`** with its own `Controllers/`, `Views/`, `_ViewImports.cshtml`. This mirrors how `Filpride` and `Bienes` are already separated.

### 1.5 DTOs (`IBS.DTOs`)

MSAP has only 3 DTO files. Move them to `IBS.DTOs/Msap/` subfolder.

---

## Phase 2: Shared Infrastructure Additions (minimal conflict risk)

Only **additive** changes to shared files. No modifications to existing members.

### 2.1 `ApplicationDbContext` — Add MSAP DbSets

```csharp
// ADD these DbSets (append-only, no existing lines changed)
public DbSet<Employee> Employees { get; set; }
public DbSet<MsapBilling> MsapBillings { get; set; }
public DbSet<MsapCollection> MsapCollections { get; set; }
public DbSet<MsapDispatchTicket> MsapDispatchTickets { get; set; }
public DbSet<MsapJobOrder> MsapJobOrders { get; set; }
public DbSet<MsapVesselSchedule> MsapVesselSchedules { get; set; }
public DbSet<MsapTariffRate> MsapTariffRates { get; set; }
public DbSet<MsapCollectionBill> MsapCollectionBills { get; set; }
public DbSet<MsapTugboatOwner> MsapTugboatOwners { get; set; }
public DbSet<MsapPort> MsapPorts { get; set; }
public DbSet<MsapPrincipal> MsapPrincipals { get; set; }
public DbSet<MsapTerminal> MsapTerminals { get; set; }
public DbSet<MsapTugboat> MsapTugboats { get; set; }
public DbSet<MsapTugMaster> MsapTugMasters { get; set; }
public DbSet<MsapUserAccess> MsapUserAccesses { get; set; }
public DbSet<MsapVessel> MsapVessels { get; set; }
public DbSet<SalesBook> SalesBooks { get; set; }
public DbSet<CashReceiptBook> CashReceiptBooks { get; set; }
```

**Conflict mitigation:** Use `Msap` prefix on all DbSet names. Even if the upstream repo adds a `Billings` DbSet, it won't collide with `MsapBillings`.

### 2.2 `IUnitOfWork` — Add MSAP repository properties

```csharp
// ADD to IUnitOfWork interface (append-only)
IMsapBillingRepository MsapBilling { get; }
IMsapCollectionRepository MsapCollection { get; }
// ... etc for all MSAP repositories
```

**Conflict mitigation:** All new members are prefixed `Msap`. Existing members untouched.

### 2.3 `UnitOfWork` — Implement MSAP repositories

```csharp
// ADD constructor parameter + field + property for each MSAP repository
// Follow existing pattern: private readonly IMsapBillingRepository _msapBilling;
// Property: public IMsapBillingRepository MsapBilling => _msapBilling;
```

### 2.4 `Program.cs` — Register MSAP services

```csharp
// ADD service registrations (append to existing block)
builder.Services.AddScoped<IMsapBillingService, MsapBillingService>();
builder.Services.AddScoped<IMsapCollectionService, MsapCollectionService>();
// ... etc
```

**Conflict mitigation:** Append to the service registration block. No existing lines change.

### 2.5 Shared Models — Handle overlap

| Model | Strategy |
|-------|----------|
| `Company` | **Reuse existing.** Both repos have identical `Company`. Do NOT add a second `MsapCompany`. |
| `ChartOfAccount` | **Reuse existing** with care. MSAP's extra fields (`FinancialStatementType`, `HasChildren`) can be added as nullable columns to the existing `FilprideChartOfAccount` or a new shared `ChartOfAccount`. Needs schema discussion. |
| `Customer` | **Reuse existing** where possible. Merge extra fields from MSAP's `Customer` into the existing `FilprideCustomer` (or create a shared base). |
| `Supplier` | Same approach as Customer. |
| `Terms` | Same approach. |
| `BankAccount` | Compare schemas. If compatible, extend existing. If not, create `MsapBankAccount`. |

**Decision needed:** For each shared model, choose one:
1. **Merge schemas** — Add MSAP-specific nullable columns to existing model (simpler queries, more migration risk)
2. **Separate tables** — Keep `FilprideCustomer` and `MsapCustomer` as separate entities (cleaner separation, more code duplication)

**Recommendation:** Merge for `Company` (identical). Separate for `Customer`/`Supplier` (schema diverged enough to cause problems).

### 2.6 Enums — Merge carefully

| Enum | Action |
|------|--------|
| `SubAccountType` | **CRITICAL:** Values are swapped (Employee=3/BankAccount=4 in MMSI vs Employee=5/BankAccount=3 in MSAP). Do NOT merge — keep separate or align with a migration. |
| `CustomerType` | Add `Government` to MSAP's version (MMSI already has it). |
| `DynamicView` | Add MSAP values to existing enum. |
| `ModuleType` | Add MSAP values to existing enum. |

**Conflict mitigation:** Only add new values, never reorder or renumber existing values.

---

## Phase 3: Area Setup (`IBSWeb/Areas/Msap`)

### 3.1 Folder Structure

```
IBSWeb/Areas/Msap/
├── Controllers/
│   ├── VesselController.cs
│   ├── VesselScheduleController.cs
│   ├── PortController.cs
│   ├── TerminalController.cs
│   ├── PrincipalController.cs
│   ├── TugboatController.cs
│   ├── TugboatOwnerController.cs
│   ├── TugMasterController.cs
│   ├── MaritimeServiceController.cs
│   ├── MaritimeReportController.cs
│   ├── BillingController.cs
│   ├── CollectionController.cs
│   ├── DispatchTicketController.cs
│   ├── JobOrderController.cs
│   ├── ServiceRequestController.cs
│   ├── TariffRateController.cs
│   ├── ChartOfAccountController.cs
│   ├── BankAccountController.cs
│   ├── MasterFileController.cs
│   ├── CompanyController.cs
│   ├── CustomerController.cs
│   ├── SupplierController.cs
│   ├── EmployeeController.cs
│   ├── PaymentTermsController.cs
│   ├── UserAccessController.cs
│   ├── AuditTrailController.cs
│   ├── DocsController.cs
│   └── MsapImportController.cs
├── Views/
│   ├── Shared/
│   └── [ControllerName]/
│       └── Index.cshtml, Create.cshtml, Edit.cshtml, etc.
├── _ViewImports.cshtml
└── _ViewStart.cshtml
```

### 3.2 Route Convention

Use area route convention (already standard in this repo):

```csharp
[Area("Msap")]
public class VesselController : Controller { ... }
```

---

## Phase 4: Migration Files

### 4.1 Strategy

- Create a **single new EF Core migration** for all MSAP schema additions
- Do NOT attempt to merge migration histories — that's asking for pain
- The new migration adds tables for MSAP-specific entities only
- Shared model changes (if any) get their own separate migration

### 4.2 Commands

```bash
dotnet ef migrations add AddMsapTables --project IBS.DataAccess --startup-project IBSWeb
```

---

## Phase 5: Verification

1. **Build:** `dotnet build "Integrated Business System.sln"` — must pass
2. **Existing tests:** Run if any exist
3. **Manual smoke test:**
   - Existing Filpride/Bienes features still work
   - MSAP area loads at `/Msap/Vessel`
   - No runtime DbContext errors
4. **Git diff check:** Verify no shared files were modified (only added to)

---

## Conflict Risk Summary

| File | Risk | Mitigation |
|------|------|------------|
| `BaseEntity.cs` | **NONE** | No changes needed |
| `Company.cs` | **NONE** | Reuse existing |
| `AppSetting.cs` | **NONE** | No changes needed |
| `ApplicationDbContext` | **LOW** | Additive-only (new DbSets) |
| `IUnitOfWork.cs` | **LOW** | Additive-only (new properties) |
| `UnitOfWork.cs` | **LOW** | Additive-only (new fields/properties) |
| `Repository.cs` | **NONE** | No changes needed |
| `MemoryCacheService.cs` | **NONE** | No changes needed |
| `Program.cs` | **LOW** | Additive-only (new registrations) |
| `Enums/General.cs` | **MEDIUM** | Add new values only, never reorder |
| `Customer`/`Supplier` models | **MEDIUM** | Separate tables to avoid schema conflicts |

---

## What Gets Skipped (for now)

- **SuperAdmin area** — MSAP's `DataController`/`HomeController`. Low value, likely replaceable with existing Admin area functionality.
- **Seed data** — MSAP's `DbSeeder`. Handle separately after migration.
- **`LocalFileStorageService`** — Dev-only. Not needed in MMSI (already has `CloudStorageService`).
- **`SalesBook`/`CashReceiptBook`** — Only if actively used. Defer to Phase 2 if not critical.

---

## Execution Order

1. Phase 1 (Namespace Isolation) — Do this first. It's pure refactoring, no behavior changes.
2. Phase 2 (Shared Infrastructure) — Additive changes only. Test after each DbSet/repository added.
3. Phase 3 (Area Setup) — Controllers + Views. Can be done in parallel with Phase 2.
4. Phase 4 (Migrations) — After all models are in place.
5. Phase 5 (Verification) — End-to-end testing.

**Estimated scope:** ~50-70 files moved/created, ~3-5 shared files with additive-only edits, 1 new migration.
