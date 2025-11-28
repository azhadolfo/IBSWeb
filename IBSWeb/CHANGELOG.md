# Changelog
All notable changes to this project will be documented in this file.

The format of this file follows **Keep a Changelog**  
and this project adheres to **Semantic Versioning (SemVer)**.

---

## [v1.2.0] - 2025-11-28
### Fixed
- Fixed redundant switch condition on the COS index.
- Fixed the CV Non-trade invoice to mark only the AP Non-Trade payable.

### Changed
- Username value when creating audit trail

## [1.0.0] - 2025-11-28
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

---

## [Unreleased]
### Added
- Added journal entries for updating commission or freight.

