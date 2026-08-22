# Changelog
All notable changes to this project will be documented in this file.

The format of this file follows **Keep a Changelog**  
and this project adheres to **Semantic Versioning (SemVer)**.

---

## [v3.2.1] - 2026-08-20

### Changed
- Updated Filpride Transaction Master Control batch rejournal so admins can run the process for a selected transaction type or for all supported transaction types in the existing sequence.

---

## [v3.2.0] - 2026-08-19

### Added
- Added a Filpride marketing approval stage for customer order slips, including marketing approver audit fields, a dedicated preview page, and the related database migration.
- Added redesigned dashboard priority and submission panels with role-aware approval queues, direct preview links, and responsive sidebar navigation support.
- Added Subsidiary Ledger trade fuel, commissionee, and hauler/freight report pages with Excel generation flows.
- Added `Supplier`, `Commission`, and `Hauler` values to the check voucher type enum for stronger trade report filtering.
- Added the Bienes company portal link to the login page portal switcher configuration.

### Changed
- Redesigned the user dashboard layout and quick-access sidebar behavior to improve mobile responsiveness, search, and navigation tracking.
- Moved the trade supplier fuel, trade supplier commissionee, and trade supplier hauler/freight report entry pages out of Accounts Payable Reports into Subsidiary Ledger Reports.
- Updated customer order slip routing and approval flow so newly submitted records go through marketing approval before CNC approval.
- Updated the start-of-month generated journal entry user label from `SYSTEM` to `SYSTEM GENERATED`.
- Updated the login page copy to make company portal switching clearer.

### Fixed
- Fixed subsidiary ledger trade fuel, commissionee, and hauler/freight reports to compute running balances correctly and prevent subtotal amounts from distorting grand totals.
- Fixed inventory and check voucher report outputs to use enum-based trade voucher type comparisons consistently, including supplier, commission, and hauler records.
- Fixed dashboard approval visibility, status labeling, company fallback handling, quick-access link detection, and navbar spacing across desktop and mobile layouts.
- Fixed the login portal navigation so the Bienes portal link resolves correctly.
- Removed the customer order slip edit restriction that blocked updates for posted periods in the current flow.

---

## [v3.1.1] - 2026-08-11

### Fixed
- Fixed the CV disbursement report format.
- Fixed the non-trade invoice report format.
- Fixed delivery receipt edit validation to restore the current reserved volume before checking remaining volume, preventing false over-allocation errors.
- Fixed the delivery receipt record lifting date prompt to show the supplier ATL number.
- Fixed accounts payable report supplier ATL output to use the supplier ATL number instead of the UPPI ATL number.

---

## [v3.1.0] - 2026-08-07

### Added
- Added the migration of blended product.

### Changed
- Modified the COS, DR, RR, AP and AR reports to adopt the changes.
---

## [v3.0.10] - 2026-08-03

### Changed
- Updated Filpride accounts payable purchase report, gross margin report, and AP report summaries and Excel exports to build product sections dynamically from the persisted transaction product names instead of fixed product buckets.
- Updated Filpride accounts receivable dispatch, sales, and COS summary report outputs to derive product columns from the persisted purchase order or customer order slip product names so historical product labels remain available even when the related product master navigation is missing or has changed.

### Fixed
- Fixed the Filpride sales report PDF and Excel summaries to keep per-customer-type and grand-total product totals keyed to `CustomerOrderSlip.ProductName`, preventing omitted or undercounted historical product amounts.
- Fixed the Filpride delivered dispatch and COS unserved-volume PDF and Excel summaries to use `PurchaseOrder.ProductName`, preserving product totals when the related product navigation is null.

---

## [v3.0.9] - 2026-07-28

### Changed
- Updated debit memo and credit memo locked-period adjustment snapshots to also store the related trade supplier from the underlying sales invoice delivery receipt purchase order when available.
- Updated Filpride sales invoice repository loading to include the related delivery receipt purchase order so supplier snapshot data is available during debit memo and credit memo approval processing.

---

## [v3.0.8] - 2026-07-28

### Added
- Added locked-period comparative adjustment support for approved debit memos and credit memos that affect prior sales invoice balances, including customer snapshot details and signed adjustment amounts in the sales comparative report dataset.

### Changed
- Updated Filpride debit memo and credit memo approval flows to automatically complete posting in the same transaction after approval, including creation of the related general ledger entries.
- Updated the Filpride debit memo and credit memo print actions to reflect the combined `Approve & Post` workflow for records pending finance approval.
- Updated the comparative sales adjustment filter to include debit memo and credit memo prior-sales effects alongside selling price adjustments.

---

## [v3.0.7] - 2026-07-28

### Added
- Added customer and supplier snapshot fields to locked-period adjustments so comparative adjustment records can retain the related party details used at the time the adjustment was recorded.

### Changed
- Updated locked-period adjustment creation flows for delivery receipt selling price, commission, freight, and receiving report unit cost updates to persist related customer and supplier snapshots when available.
- Updated the Filpride comparative adjustment PDF output to populate the existing `Customer` and `Supplier` columns from the stored locked-period adjustment snapshot fields.

---

## [v3.0.6] - 2026-06-10

### Added
- Added sub-account selection support to the Filpride Journal Voucher reclass create and edit flows, including selectable customer, supplier, bank account, and company sub-account records.

### Changed
- Updated Filpride Journal Voucher reclass entry handling to persist the selected sub-account type, record, and resolved sub-account name on each journal voucher detail line.

---

## [v3.0.5] - 2026-06-10

### Fixed
- Fixed the Filpride Sales Invoice print preview withholding tax and withholding VAT amount calculations to reuse the invoice net-of-VAT amount consistently.

---

## [v3.0.4] - 2026-06-09

### Changed
- Updated Filpride customer order slip creation to leave `ExpirationDate` blank for NPC customers as a temporary exception, while continuing to assign the standard 7-day expiration to other customers.

### Fixed
- Fixed the Filpride AP Monitoring Report export to include open purchase orders with outstanding balances from prior months, align topsheet grouping with the per-supplier worksheets, and compute unlifted quantities based on posted receiving reports through the selected month-end.
- Removed the Filpride purchase order listing restriction that hid the Admin void action for sub POs with zero received quantity and non-closed status.

---

## [v3.0.3] - 2026-06-06

### Added
- Added `EmployeeNumber` to the Filpride supplier master file so employee-category suppliers can store the employee reference directly in the supplier record.

### Changed
- Migrated Filpride employee-advance voucher flows to use `FilprideSupplier` records with `Category = Employee` as the standard payee foreign key instead of the separate employee linkage.
- Updated check voucher liquidation and related employee-advance selection flows to resolve employee advances through supplier-backed employee records.

### Removed
- Removed the check voucher `EmployeeId` linkage in favor of the standard `SupplierId` relationship plus an explicit employee-advance marker.

---

## [v3.0.2] - 2026-06-05

### Fixed
- Fixed Filpride customer order slip create and update flows to always persist `TotalAmount` from `Quantity * DeliveredPrice` instead of using the posted form value, preventing mismatched totals when the client-sent amount is stale or incorrect.

---

## [v3.0.1] - 2026-06-05

### Changed
- Added shared 4-decimal rounding helpers to the Filpride report controllers so derived numeric values can be normalized consistently before export.
- Updated Filpride accounts payable and accounts receivable report calculations to round derived raw values to 4 decimals before writing them to Excel or using them in report summaries and totals.

### Fixed
- Fixed report recalculation discrepancies caused by Excel displaying rounded values while storing higher-precision raw amounts for computed VAT, EWT, per-liter, and related derived fields.

---

## [v3.0.0] - 2026-06-04

### Added
- Added batch re-journal processing in Transaction Master Control for collection receipts, provisional receipts, debit memos, and credit memos.
- Added revised provisional receipt create, edit, and print support, including the remarks-field schema update.
- Added collection receipt detail loading in affected report and processing flows.
- Added the cleanup migration for removing unused Filpride tables after the reporting refactor.

### Changed
- Refactored Filpride inventory and re-journal handling to separate purchase GL posting from inventory processing, make the purchase GL posting async, and streamline inventory recalculation.
- Modified AP non-trade payable handling across trade and non-trade voucher flows, related forms, and report usage.
- Updated collection receipt deposit processing to use the clearing flow and revised due-date delay handling.

### Fixed
- Fixed inventory cost update journal-entry calculations and delivery receipt old-value computation.
- Fixed provisional receipt revision issues across the controller and Razor views.
- Fixed report/controller runtime issues encountered in collection receipt and accounts payable flows.
- Fixed trade payable report EWT calculations so topsheet and per-supplier outputs both use current-month per-receiving-report tax amounts consistently.
- Fixed profit and loss report sorting.
- Disabled the monthly-closure auto-reversal step for check vouchers without a DCR date.

### Removed
- Removed the old Filpride books and related report pages for cash receipt, disbursement, journal, purchase, sales, and transaction reports in SOA.

---

## [v2.9.0] - 2026-06-03

### Added
- Added locked-period adjustment recording for COS selling price, approved PO unit cost, commission, and freight changes.
- Added a reusable locked-period adjustment repository with a DTO request model for shared adjustment creation.
- Added separate comparative adjustment reports for commission and freight.

### Changed
- Revised the comparative report to use locked-period adjustments filtered by the selected month of `CreatedDate`, regardless of the original adjusted period.
- Updated comparative report output to show adjustment details including original period, document reference, old value, new value, signed adjustment amount, reason, and creator.

---

## [v2.8.1] - 2026-05-30

### Changed
- Rename the EWT ⇒ EWT Cert and WVAT ⇒ WVAT Cert in creation of CR

### Added
- Added validation to mark as required the uploading of attachment in CR if the EWT or WVAT has a value

---

## [v2.8.0] - 2026-05-29

### Changed
- Changed the API used when getting the holidays from Date.Nager to Calendarific
- Updated collection receipt deposit flow to calculate delay using DueDate + 1 as the comparison point
- Revised cost-of-money calculation to use payment components (cash, check, manager's check) instead of commission

### Added
- Added caching for the captured holidays based on the year to prevent bloating the API
- Added Deposited Date and Cleared Date columns to the collection receipt listing view
- Added ClearedDate tracking for collection receipts, with reset on return flow

---

## [v2.7.1] - 2026-05-25

### Fixed
- Fixed Filpride receiving report cost update entries for COD and prepaid purchase orders so Expanded Withholding Tax advance offsets are applied consistently during both incremental and reversal flows.
- Fixed Filpride receiving report cost update ledger entries to use the full net-of-VAT cost adjustment amount for related COGS and inventory sales postings.

---

## [v2.7.0] - 2026-05-25

### Added
- Added a Department Access configuration module for admins to manage per-action department access across Filpride entry modules.
- Added dynamic policy-based authorization backed by department access records, including the required repositories, authorization handler, policy provider, and database migrations.

### Changed
- Replaced hard-coded department authorization on Filpride entry actions with policy-based authorization for create, edit, preview, post, unpost, cancel, close, update, and related operational workflows.
- Added the Department Access admin navigation entry and updated the application version display to `v2.7.0`.

### Fixed
- Fixed Department Access listing search to avoid filtering on created and edited date fields.
- Fixed policy-based authorization so Admin users bypass department validation.
- Fixed repeated posting flows in affected modules by adding duplicate-post guards for records that were already posted.

---

## [v2.6.1] - 2026-05-19

### Changed
- Added duplicate-action guards to prevent reposting or re-approving records across affected Placement and Filpride posting workflows, including check vouchers, collection receipts, credit/debit memos, delivery receipts, journal vouchers, provisional receipts, purchase orders, receiving reports, sales invoices, and service invoices.
- Added duplicate-action guards for Filpride delivery receipt delivered and lifting-date recording flows to stop repeated processing once those actions have already been completed.

### Fixed
- Fixed the Sales Invoice posting closed-period validation message to say `Cannot post this record` instead of `Cannot unpost this record`.

---

## [v2.6.0] - 2026-05-19

### Added
- Added support for generating the Filpride inventory report for all products, including grouped product sections and per-product totals in the PDF and Excel outputs.

### Changed
- Updated the Filpride inventory report filters so product and PO selection can be left blank to generate all matching records for the selected month.

### Fixed
- Fixed Filpride dispatch report delivered `As Of` filtering to use `DeliveredDate` in the PDF and Excel outputs instead of the transaction date.
- Fixed Filpride dispatch report delivered `As Of` totals and summary sections to include only records delivered on the selected date.

---

## [v2.5.3] - 2026-05-18

### Changed
- Updated the Filpride dispatch report Delivered filter UI to use an explicit report mode selection for `As Of` and `Date Range`.
- Simplified the Filpride dispatch report date form so Delivered `As Of` uses a single date field and hides `Date To`.

### Fixed
- Fixed Filpride dispatch report Excel output to include the delivery receipt selling price from the related customer order slip after `PRODUCTS`.
- Fixed Filpride dispatch report date validation to align controller behavior with the selected Delivered report mode.

---

## [v2.5.2] - 2026-05-14

### Fixed
- Fixed Filpride trade check voucher advance setup to accumulate all available posted advance vouchers for COD and prepaid purchase orders instead of using only the first matching advance voucher.
- Fixed Filpride trade check voucher advance validation, posting, and reversal flows to support multiple referenced advance vouchers with combined available balances.

---

## [v2.5.1] - 2026-05-14

### Changed
- Expanded the `Export to AAS` navigation access in the shared layout for Filpride RCD users and admins.
- Revised the Filpride collection Excel report layout to show collection date, invoice date, check amount, EWT, WVAT, and previous/current/advance allocation columns.

### Fixed
- Fixed Filpride collection Excel totals and column placement for the revised export format, including voided receipt columns.
- Fixed Filpride collection Excel month-based allocation reporting for sales, service, and multiple-sales-invoice collections.

---

## [v2.5.0] - 2026-05-13

### Added
- Added Journal Voucher generation/report outputs for freight and commission updates.
- Added comparative report queue tracking for updated timestamps and duplicate-prevention support.

### Changed
- Finalized comparative report data processing and queue handling.
- Renamed the selling price Journal Voucher report view for consistency with the generated output.

### Fixed
- Fixed selling price GL record dates to follow the open-book check.
- Fixed commission and freight update flows to respect open-book validation.
- Fixed cost update flows to respect open-book validation.
- Fixed comparative report persistence behavior.

---

## [v2.4.0] - 2026-05-09

### Added
- Added deposited date to the Filpride collection Excel report.

### Changed
- Improved Filpride collection report export performance by using no-tracking/split queries and preloading multiple-sales-invoice details.
- Updated Filpride report Excel generation to use async package output.

### Fixed
- Fixed Filpride collection Excel report column alignment when void/cancel columns are shown.
- Fixed Filpride collection Excel report handling when no records are found.
- Fixed Dispatch Report date validation precedence for in-transit reports.

---

## [v2.3.1] - 2026-05-08
### Fixed
- Fixed inventory balance recalculation for purchase, sales, and cost update flows.
- Fixed inventory report beginning balance, average cost, total balance, and purchase order selection behavior.

---

## [v2.3.0] - 2026-03-04
### Added
- Added a navigation feature to provide an overview of tasks pending approval for CV and JV.

### Changed
- Implement enhanced amortization functionality to automatically generate a new JV each month.
- Remove the approval requirement for CV Invoice Payroll processing.

---

## [v2.2.0] - 2026-02-28
### Added
- Added quick access feature.
- Added new module for JV (Accrual, Amortization, and Reclass)
- Added approval flow for CV invoice and JV.
- Added unpost feature for JV.

### Changed
- Rename the prinout heading from "Invoicing" to "Invoicing / AP Voucher"

---

## [v2.1.2] - 2026-02-20
### Fixed
- Fixed CV payment showing not accurate payable amount.

---

## [v2.1.1] - 2026-02-18
### Fixed
- Fixed input type of payment to show the values in to 4 decimals.
- Fixed general apis to allow anonymous.

---

## [v2.1.0] - 2026-02-18
### Changed
- Redesign the CV Non Trade payment to accept partial payment.

---

## [v2.0.1] - 2026-02-13
### Fixed
- Fixed discrepancy due to rounding 4 decimals.

---

## [v2.0.0] - 2026-02-12
### Changed
- Upgrade version to.NET10.

---

## [v1.2.6] - 2025-01-17
### Added
- Implement the subaccount in journal voucher.

---

## [v1.2.5] - 2026-01-16
### Added
- Added default commissionee and commission rate to the customer file

### Changed
- Modified the date parameter needed when generating AR Per Customer.
- Revised the payroll invoice.

### Fixed
- Moved the otc fuel sales report to path correctly.

---

## [v1.2.4] - 2025-12-16
### Changed
- Modified the configuration of notification.js to low the cost of GCP.

---

## [v1.2.3] - 2025-12-04
### Added
- Added locking of database when creating new series no.

---

## [v1.2.2] - 2025-12-01
### Added
- Added journal entries for updating the commission and freight.

### Fixed
- Fixed atl booking card in dashboard not accurate.

---

## [v1.2.1] - 2025-11-29
### Changed
- Changed in to raw sql the query for getting the latest series, applied locking of row to prevent duplicate.

---

## [v1.2.0] - 2025-11-28
### Fixed
- Fixed redundant switch condition on the COS index.
- Fixed the CV Non-trade invoice to mark only the AP Non-Trade payable.

### Changed
- Username value when creating audit trail

---

## [v1.0.0] - 2025-11-28
### Added
- Initial implementation of **IBSWeb – Integrated Business System**.
- Added **N-Tier architecture** structure:
    - `IBS.DataAccess` for repositories and Unit of Work
    - `IBS.Models` for entity models
    - `IBS.DTOs` for data transfer objects
    - `IBS.Utility` for enums, constants, helpers
    - `IBS.Services` for business logic modules
    - `IBSWeb` for UI controllers and views
- Implemented **Chart of Accounts** module with hierarchical level support.
- Added **General Ledger**, **Journal Entry**, and posting logic.
- Implemented **role-based access control** (Admin, Accountant, User).
- Added **session-based authentication** support.
- Added reusable **JavaScript utilities** and global `site.js`.
- Implemented partials and modular views for accounting pages.
- Added database context configuration and initial EF Core integrations.
- Added basic **audit logging** for tracking user actions.
- Added initial documentation structure (README, repository organization).

### Changed
- Refactored repository methods to use **async/await** and cleaner LINQ.
- Improved data validation and error handling across the project.
- Updated folder naming and namespace conventions for consistency.

### Fixed
- Fixed issues in Chart of Accounts sorting and retrieval.
- Fixed session retrieval inconsistencies on user login.
- Fixed bugs in DataTables initialization and hidden column searching.
- Fixed authentication redirect issues in restricted pages.
