# Sales Network Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a sales-network module that manages multi-OEM supplier hierarchies, prospective companies, supply relationships, external contacts, and internal owner history inside the existing SWITCH KOREA sales system.

**Architecture:** Add an independent network domain that links one-to-one to existing customers while keeping company relationships, supplies, contacts, and assignments as separate records. The UI uses an approved hierarchy-browser plus detail-panel layout; React first runs against `MockSalesApi`, ASP.NET Core later implements the same contracts, and PostgreSQL is added only after backend approval.

**Tech Stack:** HTML/CSS/JavaScript mockup, React 18 + TypeScript, ASP.NET Core 10 minimal APIs, PostgreSQL 18 + EF Core 10, Vitest/Testing Library, xUnit

**Spec:** `docs/superpowers/specs/2026-10-06-sales-network-module-design.md`

## Global Constraints

- Execute phases strictly in this order: UI mockup, Front-end, Back-end, Database.
- Stop after each phase and obtain user approval before starting the next phase.
- Ask for explicit permission immediately before running any test or browser-verification command.
- All authenticated users can view the full network; members edit only companies they own, while managers edit all data and change ownership.
- A company may belong to multiple OEM networks, hierarchy depth is unlimited, and hierarchy level is computed rather than stored.
- Preserve histories through effective dates and inactive states; do not hard-delete business records.
- Reuse existing users, customers, products, audit logs, authentication, optimistic concurrency, backup, and deployment conventions.
- Do not implement Excel import, graph databases, automated research, quote/inquiry handling, or outbound notifications in this module.
- Preserve unrelated local file `deploy/installation-report.md`.

## Review Focus

- A relationship that indirectly points back to an ancestor must be rejected as a cycle; Task 5 pins this with a three-company test.
- A company appearing under two OEM roots must retain one identity and display under both roots; Tasks 2, 3, and 5 pin this.
- Converting a prospect already linked to contacts and relationships must preserve those record IDs; Tasks 5 and 8 pin this.
- Changing owners on an effective date must leave exactly one current assignment and close the prior assignment without overlap; Tasks 5 and 7 pin this.
- Two edits using the same stale `Version` must return a conflict rather than overwrite; Tasks 4, 6, and 7 pin this.

---

## Phase 1 — UI Mockup

### Task 1: Add the approved sales-network workflow to the static mockup

**Files:**
- Modify: `src/mockup/index.html`
- Modify: `src/mockup/styles.css`
- Modify: `src/mockup/mock-data.js`
- Modify: `src/mockup/app.js`
- Create: `src/mockup/network-mockup-check.mjs`

**Interfaces:**
- Consumes: existing mockup navigation, role switcher, customer/product/user sample data
- Produces: `영업망 관리` navigation target and mock interactions for tree selection, five detail tabs, company/relationship/contact forms, prospect conversion, and owner-history display

- [ ] **Step 1: Write the failing static check**

Create `network-mockup-check.mjs` using Node core `fs` assertions for these exact markers: `network-view`, `network-tree`, `network-company-detail`, `network-contact-tab`, `network-assignment-tab`, `network-history-tab`, and `convert-prospect`.

- [ ] **Step 2: Request permission and run the failing check**

Run: `node src/mockup/network-mockup-check.mjs`

Expected: FAIL because `network-view` is absent.

- [ ] **Step 3: Add mock companies and relationships**

Add sample records demonstrating Samsung and LG roots, one company linked to both, one prospect, unlimited-depth children, supply items linked to existing products, multiple external contacts, and historical internal assignments.

- [ ] **Step 4: Build the approved hierarchy-browser plus detail-panel screen**

Add top filters, collapsible hierarchy, company badges, the five approved tabs, permission-aware actions, contact/relationship modals, and a prospect-conversion flow. Keep existing SWITCH KOREA colors and responsive behavior.

- [ ] **Step 5: Request permission and verify the mockup**

Run:

```powershell
node --check src/mockup/app.js
node src/mockup/network-mockup-check.mjs
```

Expected: JavaScript syntax PASS and `NETWORK_MOCKUP_CHECK=PASS`.

- [ ] **Step 6: Perform approved browser verification**

Verify navigation, expand/collapse, company selection, all five tabs, prospect conversion, manager/member controls, and narrow-width layout. Record any visual defects before claiming the phase complete.

- [ ] **Step 7: Commit the UI mockup**

```powershell
git add src/mockup/index.html src/mockup/styles.css src/mockup/mock-data.js src/mockup/app.js src/mockup/network-mockup-check.mjs
git commit -m "feat: add sales network UI mockup"
```

- [ ] **Step 8: Stop and obtain UI-mockup approval**

Do not start Front-end work until the user explicitly approves this phase.

---

## Phase 2 — Front-end

### Task 2: Define front-end network contracts and mock API behavior

**Files:**
- Modify: `src/frontend/src/shared/api/contracts.ts`
- Modify: `src/frontend/src/shared/api/SalesApi.ts`
- Modify: `src/frontend/src/shared/api/MockSalesApi.ts`
- Modify: `src/frontend/src/shared/api/HttpSalesApi.ts`
- Create: `src/frontend/tests/network-api.test.ts`
- Modify: `src/frontend/tests/http-sales-api.test.ts`

**Interfaces:**
- Consumes: existing `User`, `Customer`, `Product`, `ActiveStatus`, `ListQuery`, and optimistic `version` conventions
- Produces: `NetworkCompany`, `CompanyRelationship`, `RelationshipSupply`, `CompanyContact`, `CompanyAssignment`, `NetworkTreeNode`, `NetworkQuery`, `NetworkCompanyInput`, `RelationshipInput`, `ContactInput`, `AssignmentChangeInput`; matching `SalesApi` list/get/create/update/disable/convert/tree methods and HTTP route mappings

- [ ] **Step 1: Write failing mock API tests**

Add tests named:

- `lists_one_company_under_two_oem_roots_without_duplicating_identity`
- `converts_a_prospect_without_losing_contacts_or_relationships`
- `rejects_member_update_for_another_owners_company`

Assert stable company/contact/relationship IDs and a `SalesApiError(403)` for the forbidden edit.
Extend `http-sales-api.test.ts` to assert exact `/api/network` methods, routes, query names, JSON property names, and `expectedVersion` fields.

- [ ] **Step 2: Request permission and run the focused test**

Run: `src\frontend\node_modules\.bin\vitest.CMD run tests\network-api.test.ts tests\http-sales-api.test.ts`

Expected: FAIL because network contracts and methods do not exist.

- [ ] **Step 3: Add exact TypeScript contracts**

Use `lifecycleType: 'customer' | 'prospect'`, `isOem: boolean`, nullable `customerId`, effective-date strings in `YYYY-MM-DD`, and `version: number`. Define `NetworkTreeNode` with `company`, `depth`, `pathCompanyIds`, and `children`.

- [ ] **Step 4: Extend `SalesApi`, `MockSalesApi`, and `HttpSalesApi`**

Implement in-memory filtering, multi-root tree construction with per-path cycle protection, permission checks, history-preserving prospect conversion, and optimistic-version conflict behavior.
Map the real client to the six spec routes while preserving the approved TypeScript contracts; the backend may return 404 during this phase and is not called by the mock-data browser review.

- [ ] **Step 5: Request permission and run the focused test**

Run: `src\frontend\node_modules\.bin\vitest.CMD run tests\network-api.test.ts tests\http-sales-api.test.ts`

Expected: PASS.

- [ ] **Step 6: Commit contracts and mock behavior**

```powershell
git add src/frontend/src/shared/api/contracts.ts src/frontend/src/shared/api/SalesApi.ts src/frontend/src/shared/api/MockSalesApi.ts src/frontend/src/shared/api/HttpSalesApi.ts src/frontend/tests/network-api.test.ts src/frontend/tests/http-sales-api.test.ts
git commit -m "feat: add sales network frontend contracts"
```

### Task 3: Implement the React network workspace

**Files:**
- Create: `src/frontend/src/features/network/NetworkPage.tsx`
- Create: `src/frontend/src/features/network/NetworkFilters.tsx`
- Create: `src/frontend/src/features/network/NetworkTree.tsx`
- Create: `src/frontend/src/features/network/CompanyDetailPanel.tsx`
- Create: `src/frontend/src/features/network/RelationshipEditor.tsx`
- Create: `src/frontend/src/features/network/ContactEditor.tsx`
- Create: `src/frontend/src/features/network/AssignmentHistory.tsx`
- Modify: `src/frontend/src/app/App.tsx`
- Modify: `src/frontend/src/app/styles.css`
- Create: `src/frontend/tests/network-page.test.tsx`
- Modify: `src/frontend/tests/app-routing.test.tsx`

**Interfaces:**
- Consumes: all Task 2 contracts and `SalesApi` methods, `useAuth`, `PermissionGate`, `ConcurrencyNotice`
- Produces: authenticated `/network` route and approved hierarchy-browser/detail-panel interaction

- [ ] **Step 1: Write failing screen tests**

Test these behaviors: `영업망 관리` appears in navigation; Samsung and LG roots can both show the same company ID; selecting a node updates the detail panel; five tabs render; members cannot see edit actions for another owner; managers can see conversion and owner-change actions.

- [ ] **Step 2: Request permission and run focused tests**

Run: `src\frontend\node_modules\.bin\vitest.CMD run tests\network-page.test.tsx tests\app-routing.test.tsx`

Expected: FAIL because the route and page do not exist.

- [ ] **Step 3: Implement the route, filters, and tree**

Keep the selected root, filters, and selected company in URL search parameters. Render repeated tree placements with a stable `company.id` and a path-based React key.

- [ ] **Step 4: Implement detail tabs and editors**

Add approved tabs, relationship/supply editing, contact history, assignment history, conversion UI, inline validation, `409` concurrency notice, `422` relationship messages, and inactive-state actions.

- [ ] **Step 5: Request permission and run front-end verification**

Run:

```powershell
src\frontend\node_modules\.bin\vitest.CMD run
src\frontend\node_modules\.bin\tsc.CMD -b
src\frontend\node_modules\.bin\vite.CMD build
```

Expected: all Vitest files PASS and production build succeeds. Record the existing ESLint configuration limitation separately unless an ESLint config has been added.

- [ ] **Step 6: Perform approved browser verification with mock data**

Verify the full manager/member workflow at desktop and narrow widths. Do not connect to the live API in this phase.

- [ ] **Step 7: Commit the front-end**

```powershell
git add src/frontend/src/features/network src/frontend/src/app/App.tsx src/frontend/src/app/styles.css src/frontend/tests/network-page.test.tsx src/frontend/tests/app-routing.test.tsx
git commit -m "feat: add sales network frontend"
```

- [ ] **Step 8: Stop and obtain Front-end approval**

Do not start Back-end work until the user explicitly approves this phase.

---

## Phase 3 — Back-end

### Task 4: Add network domain models, repository interfaces, and authorization rules

**Files:**
- Modify: `src/backend/SalesManagement.Core/Models/Enums.cs`
- Create: `src/backend/SalesManagement.Core/Models/NetworkCompany.cs`
- Create: `src/backend/SalesManagement.Core/Models/CompanyRelationship.cs`
- Create: `src/backend/SalesManagement.Core/Models/RelationshipSupply.cs`
- Create: `src/backend/SalesManagement.Core/Models/CompanyContact.cs`
- Create: `src/backend/SalesManagement.Core/Models/CompanyAssignment.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/INetworkCompanyRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/ICompanyRelationshipRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/ICompanyContactRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/ICompanyAssignmentRepository.cs`
- Modify: `src/backend/SalesManagement.Core/Auth/AuthorizationService.cs`
- Modify: `src/backend/SalesManagement.InMemory/Repositories/InMemoryRepositories.cs`
- Create: `src/backend/SalesManagement.Tests/NetworkAuthorizationTests.cs`

**Interfaces:**
- Consumes: `EntityBase`, `User`, `Customer`, `Product`, `IRepository<T>`, manager/member roles
- Produces: domain entity types and repository query methods `ListByOwnerAsync`, `ListByCompanyAsync`, `ListByRootAsync`, and authorization method `EnsureCanEditNetworkCompany(User actor, NetworkCompany company, CompanyAssignment? currentAssignment)`

- [ ] **Step 1: Write failing authorization and model tests**

Assert member-own edit succeeds, another-owner edit throws `ForbiddenException`, manager edit succeeds, and updating with a stale expected version throws `ConcurrencyException` in the in-memory repository.

- [ ] **Step 2: Request permission and run focused tests**

Run: `dotnet test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj --filter FullyQualifiedName~NetworkAuthorizationTests`

Expected: FAIL because network types do not exist.

- [ ] **Step 3: Add domain models and repository interfaces**

Use `LifecycleType.Customer|Prospect`, `EmploymentStatus.Active|Former`, nullable end dates, `Status`, and `Version`. Do not store hierarchy depth.

- [ ] **Step 4: Add authorization and in-memory repositories**

Modify `src/backend/SalesManagement.InMemory/Repositories/InMemoryRepositories.cs` to implement the four interfaces while preserving expected-version behavior.

- [ ] **Step 5: Request permission and run focused tests**

Run the Task 4 test command; expected PASS.

- [ ] **Step 6: Commit the domain foundation**

```powershell
git add src/backend/SalesManagement.Core src/backend/SalesManagement.InMemory/Repositories/InMemoryRepositories.cs src/backend/SalesManagement.Tests/NetworkAuthorizationTests.cs
git commit -m "feat: add sales network domain model"
```

### Task 5: Implement network workflows and validation

**Files:**
- Create: `src/backend/SalesManagement.Core/Network/NetworkCompanyService.cs`
- Create: `src/backend/SalesManagement.Core/Network/NetworkRelationshipService.cs`
- Create: `src/backend/SalesManagement.Core/Network/NetworkTreeService.cs`
- Create: `src/backend/SalesManagement.Core/Network/CompanyContactService.cs`
- Create: `src/backend/SalesManagement.Core/Network/CompanyAssignmentService.cs`
- Create: `src/backend/SalesManagement.Core/Exceptions/NetworkExceptions.cs`
- Create: `src/backend/SalesManagement.Tests/NetworkWorkflowTests.cs`

**Interfaces:**
- Consumes: Task 4 repositories, `ICustomerRepository`, `IProductRepository`, `IUserRepository`, `IAuditRepository`, `ITransactionRunner`
- Produces: `SaveCompanyAsync`, `ConvertProspectAsync`, `SaveRelationshipAsync`, `BuildTreeAsync`, `SaveContactAsync`, and `ChangeAssignmentAsync` service methods with actor, expected-version, and cancellation-token parameters

- [ ] **Step 1: Write failing workflow tests**

Add tests for a three-node cycle rejection, overlapping duplicate relationship rejection, the same company under two OEM roots, prospect conversion preserving record IDs, one-current-assignment enforcement, non-overlapping owner history, and invalid date ranges.

- [ ] **Step 2: Request permission and run focused tests**

Run: `dotnet test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj --filter FullyQualifiedName~NetworkWorkflowTests`

Expected: FAIL because services do not exist.

- [ ] **Step 3: Implement relationship and tree services**

Use adjacency lists and iterative depth-first traversal with a per-path visited set. Reject a new edge when its downstream company already reaches the proposed upstream company.

- [ ] **Step 4: Implement company, conversion, contact, and assignment services**

Run conversion and owner change through `ITransactionRunner.ExecuteAsync<T>`. Prospect conversion creates one `Customer`, links `CustomerId`, preserves related IDs, and writes one audit entry without sensitive contact values beyond the changed business fields.

- [ ] **Step 5: Request permission and run focused tests**

Run the Task 5 test command; expected PASS.

- [ ] **Step 6: Commit workflows**

```powershell
git add src/backend/SalesManagement.Core/Network src/backend/SalesManagement.Core/Exceptions/NetworkExceptions.cs src/backend/SalesManagement.Tests/NetworkWorkflowTests.cs
git commit -m "feat: implement sales network workflows"
```

### Task 6: Expose network APIs that match the approved front-end contract

**Files:**
- Create: `src/backend/SalesManagement.Api/Endpoints/NetworkEndpoints.cs`
- Modify: `src/backend/SalesManagement.Api/Program.cs`
- Modify: `src/backend/SalesManagement.Api/Configuration/ProductionRuntimeExtensions.cs`
- Create: `src/backend/SalesManagement.Tests/NetworkContractTests.cs`

**Interfaces:**
- Consumes: Task 5 services and Task 2 HTTP route contract
- Produces: `/api/network/companies`, `/api/network/companies/{id}/convert`, `/api/network/relationships`, `/api/network/companies/{id}/contacts`, `/api/network/companies/{id}/assignments`, and `/api/network/tree`

- [ ] **Step 1: Write failing API-contract tests**

Assert exact methods, routes, JSON property names, status mappings (`403`, `404`, `409`, `422`), filter query names, and `expectedVersion` request fields match Task 2.

- [ ] **Step 2: Request permission and run focused tests**

Run the backend contract filter; expected FAIL for missing endpoints.

- [ ] **Step 3: Map endpoints and exception responses**

Reuse the authenticated actor pattern. Register services and in-memory repositories for development. Map network exceptions to user-actionable problem details without leaking contact data.

- [ ] **Step 4: Match endpoint responses to the approved front-end contract**

Map enums to lowercase TypeScript unions, ISO dates to `YYYY-MM-DD`, and backend owner/user IDs to the exact Task 2 property names.

- [ ] **Step 5: Request permission and run backend/front-end tests**

Run focused tests followed by the existing backend unit suite and full front-end suite. Expected: all relevant tests PASS; document Windows-only test skips or environment blockers rather than hiding them.

- [ ] **Step 6: Commit API integration**

```powershell
git add src/backend/SalesManagement.Api src/backend/SalesManagement.Tests/NetworkContractTests.cs
git commit -m "feat: expose sales network APIs"
```

- [ ] **Step 7: Stop and obtain Back-end approval**

Demonstrate behavior against in-memory storage only. Do not create migrations or modify PostgreSQL before approval.

---

## Phase 4 — Database

### Task 7: Add PostgreSQL mappings, repositories, and constraints

**Files:**
- Modify: `src/backend/SalesManagement.Postgres/SalesDbContext.cs`
- Create: `src/backend/SalesManagement.Postgres/Configurations/NetworkConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/NetworkCompanyRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/CompanyRelationshipRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/CompanyContactRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/CompanyAssignmentRepository.cs`
- Modify: `src/backend/SalesManagement.Postgres/PostgresServiceExtensions.cs`
- Modify: `src/backend/SalesManagement.Api/Configuration/ProductionRuntimeExtensions.cs`
- Modify: `src/backend/SalesManagement.DbTests/PostgresConstraintTests.cs`

**Interfaces:**
- Consumes: Task 4 repository interfaces and entities
- Produces: EF Core mappings and PostgreSQL repository implementations with the same service-visible behavior

- [ ] **Step 1: Write failing database constraint tests**

Test unique non-null `CustomerId`, unique active upstream/downstream/period relationship, one current assignment per company, required foreign keys, stale-version conflict, and assignment-date overlap rejection.

- [ ] **Step 2: Request permission and run database tests**

Run: `dotnet test src/backend/SalesManagement.DbTests/SalesManagement.DbTests.csproj --filter FullyQualifiedName~PostgresConstraintTests`

Expected: FAIL because tables and mappings are absent. If the PostgreSQL test container is unavailable, report the blocker before continuing.

- [ ] **Step 3: Add DbSets, mappings, indexes, and repositories**

Use snake-case table names, restrictive deletes for business-history records, partial unique indexes for non-null `CustomerId` and current assignments, and existing concurrency-token conventions.

- [ ] **Step 4: Register PostgreSQL implementations**

Production resolves all four network repository interfaces to PostgreSQL; development keeps in-memory repositories.

- [ ] **Step 5: Request permission and rerun database tests**

Expected: focused database constraints PASS.

- [ ] **Step 6: Commit persistence mappings**

```powershell
git add src/backend/SalesManagement.Postgres src/backend/SalesManagement.Api/Configuration/ProductionRuntimeExtensions.cs src/backend/SalesManagement.DbTests/PostgresConstraintTests.cs
git commit -m "feat: persist sales network data"
```

### Task 8: Create migration, backfill existing customers, and verify end to end

**Files:**
- Create: `src/backend/SalesManagement.Postgres/Migrations/20261006090000_AddSalesNetwork.cs`
- Create: `src/backend/SalesManagement.Postgres/Migrations/20261006090000_AddSalesNetwork.Designer.cs`
- Modify: `src/backend/SalesManagement.Postgres/Migrations/SalesDbContextModelSnapshot.cs`
- Modify: `src/backend/SalesManagement.DbTests/EndToEndTests.cs`
- Modify: `Dockerfile`
- Modify: `docs/superpowers/specs/2026-10-02-temporary-cloud-test-deployment-design.md` only if deployment operations change

**Interfaces:**
- Consumes: Task 7 mappings and existing customer/user/product data
- Produces: idempotent schema migration, one `NetworkCompany` and one current `CompanyAssignment` for every existing customer, and a deployable integrated application

- [ ] **Step 1: Write the failing migration/backfill scenario**

Seed two existing customers, apply the migration/backfill, and assert exactly two linked network companies, correct current owners, preserved customer IDs, and no duplicate rows on a second initialization pass. Add an end-to-end scenario that creates a prospect, links it under Samsung and LG, adds a contact, converts it, changes owner, and reloads all histories.

- [ ] **Step 2: Request permission and run the scenario before migration**

Expected: FAIL because the migration and backfill do not exist.

- [ ] **Step 3: Generate and review the EF migration**

Generate `AddSalesNetwork`, add deterministic existing-customer backfill, and review SQL for data preservation, foreign-key order, partial indexes, and rollback safety. Do not apply it to Render yet.

- [ ] **Step 4: Request permission and run complete verification**

Run approved front-end, backend, database, build, migration, and end-to-end commands. Expected: all module tests PASS and production artifacts build.

- [ ] **Step 5: Commit migration and integrated verification**

```powershell
git add src/backend/SalesManagement.Postgres/Migrations src/backend/SalesManagement.DbTests/EndToEndTests.cs Dockerfile
git commit -m "feat: migrate sales network module"
```

- [ ] **Step 6: Obtain explicit deployment approval**

Report migration scope, backup state, rollback plan, test evidence, and target Render service. Do not deploy before approval.

- [ ] **Step 7: Deploy and verify the live module**

After approval, push the detached HEAD explicitly, monitor Render until `live`, verify `/health/ready`, test manager/member permissions without exposing credentials, and perform browser verification of the integrated workflow.

- [ ] **Step 8: Obtain final Database/module approval**

Provide live URL, commit, test evidence, known limitations, and confirm that unrelated local files remain untouched.
