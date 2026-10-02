# Temporary Cloud Test Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deploy the SWITCH KOREA sales system to a temporary, internet-accessible Render HTTPS environment so five approved users can test login, authorization, data entry, and dashboard behavior before the intranet installation.

**Architecture:** A multi-stage Docker image builds the React client and .NET 10 API into one Render Web Service. Render PostgreSQL stores test data and ASP.NET Core data-protection keys; a private GitHub repository supplies the source, while a local bootstrap script performs migrations and creates one-time credentials without exposing secrets to Git or Render logs.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10.0.12, Npgsql 10.0.3, React 18, Vite 6, pnpm, Docker, PostgreSQL 18, Render Blueprint, private GitHub repository, PowerShell 5.1-compatible deployment scripts

**Spec:** `docs/superpowers/specs/2026-10-02-temporary-cloud-test-deployment-design.md`

## Global Constraints

- This is a temporary test environment, not production; its maximum lifetime is 30 days.
- Use only fictional customer, contact, product, price, shipment, quote, and opportunity data.
- The GitHub repository must be private, and no password, connection URL, cookie key, token, or initial credential may enter Git or deployment logs.
- The public entry point is the Render-provided `onrender.com` HTTPS URL; use Render's automatic `RENDER_EXTERNAL_HOSTNAME` value and require no custom domain.
- The Web Service and PostgreSQL plans are `free`; first-request wake-up delay and database restart/unavailability are accepted test limitations.
- Five initial accounts must use the exact approved IDs, names, roles, and emails from the spec; generated passwords must be unique and existing passwords must never be reset by redeployment.
- The combined container must serve the React client and API from one origin and listen on Render's `PORT`.
- Render PostgreSQL URL input must be normalized to an Npgsql key/value connection string without logging credentials.
- Windows intranet deployment behavior must remain intact; cloud support is an explicit `Render` hosting mode, not a replacement.
- GitHub and Render sign-up, login, OAuth authorization, and security challenges are performed manually by the user.
- External push, Blueprint creation, and resource deletion are explicit deployment actions and must target only the approved temporary test resources.

## Review Focus

- A percent-encoded database username, password, or database name in `DATABASE_URL` must decode correctly without appearing in an exception or log; covered by Task 1.
- An untrusted or malformed forwarded-host/protocol header must not bypass HTTPS or poison generated URLs; covered by Task 2.
- Container restart or redeploy must preserve cookie data-protection keys in PostgreSQL and may sign users out only when the key table is unavailable; covered by Task 2.
- A failed migration or unreachable database must stop container startup and keep Render readiness unhealthy rather than serving a partially initialized UI; covered by Tasks 3 and 4.
- A second bootstrap run must preserve account hashes and must not overwrite or recreate the one-time credential file; covered by Task 4.

---

### Task 1: Render PostgreSQL URL Normalization

**Files:**
- Create: `src/backend/SalesManagement.Postgres/Configuration/RenderPostgresConnectionString.cs`
- Modify: `src/backend/SalesManagement.Api/Configuration/ProductionRuntimeOptions.cs`
- Modify: `src/backend/SalesManagement.Setup/Program.cs`
- Create: `src/backend/SalesManagement.Tests/RenderPostgresConnectionStringTests.cs`

**Interfaces:**
- Consumes: `DATABASE_URL` in PostgreSQL URI form and the existing `ConnectionStrings__SalesManagement` / `SWITCHKOREA_CONNECTION_FILE` inputs.
- Produces: `SalesManagement.Postgres.Configuration.RenderPostgresConnectionString.Normalize(string databaseUrl): string`; `ProductionRuntimeOptions.From(IConfiguration)` and the Setup CLI use `DATABASE_URL` only when their existing .NET connection input is absent.

- [ ] **Step 1: Write the failing URL-normalization tests**

Add tests named `Normalize_decodes_render_url_into_npgsql_fields`, `Normalize_rejects_non_postgres_or_missing_components_without_echoing_secret`, and `Existing_dotnet_connection_string_takes_precedence_over_database_url`. Assert literal Host, Port, Database, Username, Password, and `Persist Security Info=false` values through `NpgsqlConnectionStringBuilder`; use a password containing percent-encoded `@`, `:`, and `/` characters and assert it never appears in exception text.

- [ ] **Step 2: Run the focused tests and verify RED**

Run: `dotnet test src/backend/SalesManagement.Tests --filter RenderPostgresConnectionStringTests`

Expected: FAIL because `RenderPostgresConnectionString` and `DATABASE_URL` support do not exist.

- [ ] **Step 3: Implement the normalizer and precedence rule**

Implement `Normalize(string databaseUrl): string` in the PostgreSQL project with `Uri`, explicit `postgres`/`postgresql` scheme validation, percent-decoding, required host/user/database checks, port default `5432`, and `NpgsqlConnectionStringBuilder`. Extend `ProductionRuntimeOptions` with `DatabaseUrlVariable = "DATABASE_URL"`; preserve current Windows connection string and DPAPI file precedence. Make Setup resolve `ConnectionStrings__SalesManagement` first and `DATABASE_URL` second.

- [ ] **Step 4: Run focused and regression tests**

Run: `dotnet test src/backend/SalesManagement.Tests --filter RenderPostgresConnectionStringTests`

Run: `dotnet test src/backend/SalesManagement.sln`

Expected: all tests PASS and no output contains the test password.

- [ ] **Step 5: Commit**

```powershell
git add src/backend
git commit -m "feat: accept secure Render PostgreSQL URLs"
```

### Task 2: Cross-Platform Production Runtime and Proxy HTTPS

**Files:**
- Modify: `src/backend/SalesManagement.Api/Configuration/ProductionRuntimeOptions.cs`
- Modify: `src/backend/SalesManagement.Api/Configuration/ProductionRuntimeExtensions.cs`
- Modify: `src/backend/SalesManagement.Api/Program.cs`
- Modify: `src/backend/SalesManagement.Api/appsettings.json`
- Modify: `src/backend/SalesManagement.Postgres/SalesDbContext.cs`
- Modify: `src/backend/SalesManagement.Postgres/SalesManagement.Postgres.csproj`
- Create: `src/backend/SalesManagement.Postgres/Migrations/20261002000000_AddDataProtectionKeys.cs`
- Modify: `src/backend/SalesManagement.Postgres/Migrations/SalesDbContextModelSnapshot.cs`
- Modify: `src/backend/SalesManagement.Tests/ProductionRuntimeTests.cs`
- Create: `src/backend/SalesManagement.Tests/RenderProxyTests.cs`

**Interfaces:**
- Consumes: Task 1 resolved connection string, `SWITCHKOREA_HOSTING_MODE` with allowed values `Windows` and `Render`, and Render's automatic `RENDER_EXTERNAL_HOSTNAME`.
- Produces: `ProductionHostingMode` enum; Windows mode keeps filesystem + DPAPI keys, Render mode uses `PersistKeysToDbContext<SalesDbContext>()`; proxy-aware HTTPS request pipeline.

- [ ] **Step 1: Write failing cloud-runtime and proxy tests**

Add tests named `Render_mode_uses_PostgreSql_data_protection_without_Windows_DPAPI`, `Unknown_production_hosting_mode_is_rejected`, `Render_forwarded_https_request_does_not_redirect`, and `Untrusted_host_is_rejected`. Assert Render mode can build services on a non-Windows host without `SWITCHKOREA_DATA_PROTECTION_PATH`, that the EF model contains `DataProtectionKeys`, and that a request with the configured Render host and forwarded HTTPS reaches `/health` without an HTTP redirect loop.

- [ ] **Step 2: Run focused tests and verify RED**

Run: `dotnet test src/backend/SalesManagement.Tests --filter "FullyQualifiedName~ProductionRuntimeTests|FullyQualifiedName~RenderProxyTests"`

Expected: FAIL because Production rejects non-Windows hosts, the key table and hosting mode are absent, and forwarded headers are not applied.

- [ ] **Step 3: Implement hosting-mode-specific key persistence**

Add `ProductionHostingMode { Windows, Render }` and parse `SWITCHKOREA_HOSTING_MODE`, defaulting to `Windows`. Make `SalesDbContext` implement `IDataProtectionKeyContext`, add `DbSet<DataProtectionKey> DataProtectionKeys`, reference `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` version `10.0.12`, and generate the EF migration. Keep the current DPAPI path requirement in Windows mode; in Render mode persist keys to PostgreSQL with application name `SwitchKorea.SalesManagement`.

- [ ] **Step 4: Implement Render proxy and host handling**

Register forwarded headers for `X-Forwarded-For` and `X-Forwarded-Proto`, apply them before HSTS/HTTPS redirection, and accept only the exact hostname supplied by `RENDER_EXTERNAL_HOSTNAME` plus localhost health-test hosts. Production Render mode must reject an empty public host. Do not use `AllowedHosts=*`.

- [ ] **Step 5: Run focused tests, full tests, and migration validation**

Run: `dotnet test src/backend/SalesManagement.Tests --filter "FullyQualifiedName~ProductionRuntimeTests|FullyQualifiedName~RenderProxyTests"`

Run: `dotnet test src/backend/SalesManagement.sln`

Run: `dotnet ef migrations has-pending-model-changes --project src/backend/SalesManagement.Postgres --startup-project src/backend/SalesManagement.Api`

Expected: all tests PASS and EF reports no pending model changes.

- [ ] **Step 6: Commit**

```powershell
git add src/backend
git commit -m "feat: support secure Render production runtime"
```

### Task 3: Reproducible Combined Docker Image

**Files:**
- Create: `Dockerfile`
- Create: `.dockerignore`
- Create: `deploy/render/start.sh`
- Create: `deploy/tests/ContainerDeployment.Tests.ps1`
- Modify: `.gitignore`
- Modify: `deploy/runbook.md`

**Interfaces:**
- Consumes: `DATABASE_URL`, `PORT`, `SWITCHKOREA_HOSTING_MODE=Render`, automatic `RENDER_EXTERNAL_HOSTNAME`, frontend `pnpm-lock.yaml`, and Tasks 1–2 runtime.
- Produces: Linux .NET 10 image whose entrypoint runs `SalesManagement.Setup --migrate-only` and then `SalesManagement.Api` on `http://0.0.0.0:${PORT}` with the frontend under `wwwroot`.

- [ ] **Step 1: Write the failing container behavior checks**

`ContainerDeployment.Tests.ps1` must build the image, run it against a disposable PostgreSQL container, wait for `/health/ready`, assert `/` returns the React entry page, assert `/health` contains no secret, stop and restart the application container against the same database, and assert readiness returns again. It must also scan the final image history and exported environment for the fixture password.

- [ ] **Step 2: Run the checks and verify RED**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/ContainerDeployment.Tests.ps1`

Expected: FAIL because `Dockerfile`, `.dockerignore`, and the Render entrypoint do not exist.

- [ ] **Step 3: Implement the multi-stage image**

Use pinned Node 22, .NET SDK 10.0, and ASP.NET 10.0 image tags. Build the frontend with `corepack` and `pnpm install --frozen-lockfile`; publish API and Setup separately; copy `dist` to API `wwwroot`; run as a non-root user; expose no fixed public port; include only published artifacts and `start.sh` in the runtime stage.

- [ ] **Step 4: Implement fail-fast startup**

`start.sh` requires `DATABASE_URL`, `PORT`, and `RENDER_EXTERNAL_HOSTNAME`, runs the setup CLI with `--migrate-only`, and uses `exec` for the API process. It must never echo environment values and must exit nonzero on migration failure.

- [ ] **Step 5: Run container checks and local regressions**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/ContainerDeployment.Tests.ps1`

Run: `pnpm --dir src/frontend test`

Run: `pnpm --dir src/frontend build`

Run: `dotnet test src/backend/SalesManagement.sln`

Expected: container smoke checks PASS, frontend tests/build PASS, and backend tests PASS.

- [ ] **Step 6: Commit**

```powershell
git add Dockerfile .dockerignore .gitignore deploy src
git commit -m "feat: package combined sales app container"
```

### Task 4: Render Blueprint and Safe Account Bootstrap

**Files:**
- Create: `render.yaml`
- Create: `deploy/scripts/Initialize-RenderTestEnvironment.ps1`
- Create: `deploy/tests/RenderDeployment.Tests.ps1`
- Modify: `deploy/runbook.md`

**Interfaces:**
- Consumes: Task 3 image, local environment variable `RENDER_DATABASE_URL`, existing `SalesManagement.Setup`, and the five approved account definitions.
- Produces: validated Render Blueprint for `switch-korea-sales-test` and `switch-korea-sales-test-db`; local-only `C:\SwitchKorea\Secrets\render-test-initial-credentials.txt`.

- [ ] **Step 1: Write failing Blueprint and bootstrap tests**

Test the Blueprint with the Render schema/CLI and assert the effective plan creates exactly one Docker Web Service and one PostgreSQL 18 database in `singapore`, both on `free`, with `/health/ready`, `DATABASE_URL` sourced from the database, and `SWITCHKOREA_HOSTING_MODE=Render`; `RENDER_EXTERNAL_HOSTNAME` is platform-supplied and must not be hardcoded. With a fake setup runner, assert bootstrap rejects a missing/non-PostgreSQL URL, passes the URL only through process environment, masks it in output, writes under the approved local secrets directory, and a second run reports five existing accounts without changing the credential file.

- [ ] **Step 2: Run the focused tests and verify RED**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/RenderDeployment.Tests.ps1`

Expected: FAIL because the Blueprint and bootstrap script do not exist.

- [ ] **Step 3: Implement and validate `render.yaml`**

Define `runtime: docker`, `plan: free`, `dockerfilePath: ./Dockerfile`, `healthCheckPath: /health/ready`, `region: singapore`, and PostgreSQL `postgresMajorVersion: '18'`. Use `fromDatabase.property: connectionString` for `DATABASE_URL`; rely on Render's automatic `RENDER_EXTERNAL_HOSTNAME`; never hardcode credentials or tokens.

- [ ] **Step 4: Implement the local account bootstrap**

`Initialize-RenderTestEnvironment.ps1` reads `RENDER_DATABASE_URL` from the current process, passes it to the child process only through `DATABASE_URL`, invokes the Setup project with `--migrate --credentials-path C:\SwitchKorea\Secrets\render-test-initial-credentials.txt`, and verifies the file ACL and exact five login IDs. If the credential file already exists, refuse to overwrite it; allow an idempotence verification mode that expects `created=0, existing=5`.

- [ ] **Step 5: Run focused tests and secret scans**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/RenderDeployment.Tests.ps1`

Run: `rg -n -i "postgres(ql)?://[^ ]+:[^ ]+@|password\s*=|github[_-]?token|render[_-]?api[_-]?key" --glob '!docs/**' --glob '!*.Tests.*' .`

Expected: deployment tests PASS and no committed secret assignment is found.

- [ ] **Step 6: Commit**

```powershell
git add render.yaml deploy
git commit -m "feat: define temporary Render test environment"
```

### Task 5: Private Repository, Live Deployment, and Test Handoff

**Files:**
- Create: `deploy/render-test-report.md`
- Create: `deploy/render-test-checklist.md`
- Modify: `deploy/runbook.md`

**Interfaces:**
- Consumes: Tasks 1–4, user-authenticated GitHub and Render sessions, private repository URL, Render external database URL, and Render public hostname.
- Produces: live HTTPS test URL, five one-time credentials stored only on the local PC, verified test evidence, user test checklist, and documented teardown targets.

- [ ] **Step 1: Run the complete pre-deployment regression**

Run the frontend test/build, backend solution tests, container deployment tests, Render deployment tests, and plaintext-secret scan from Tasks 3–4.

Expected: every suite PASS before any external push or cloud resource creation.

- [ ] **Step 2: Complete manual account authentication**

Open GitHub and Render sign-up/login pages for the user. The user completes email verification, passwords, MFA/security challenges, and Render-to-GitHub OAuth authorization manually. Record no credentials, cookies, recovery codes, or access tokens in project files or chat.

- [ ] **Step 3: Create and verify the private GitHub repository**

Create one repository named `switch-korea-sales-test`, verify visibility is `PRIVATE`, add it as the test remote, and push only the approved deployment branch. Verify the remote repository contains no ignored artifacts, credential file, `.env`, database dump, DPAPI file, or secret scan finding.

- [ ] **Step 4: Create the Render Blueprint resources**

Create a Blueprint from the private repository, verify the preview contains only `switch-korea-sales-test` and `switch-korea-sales-test-db`, and apply. Confirm `RENDER_EXTERNAL_HOSTNAME` matches the generated public hostname. Record service/database IDs and the 30-day database expiry date in `deploy/render-test-report.md`; record no secret values.

- [ ] **Step 5: Bootstrap the five accounts locally**

Temporarily allow only this PC's current public IP for the Render database, copy the external PostgreSQL URL into the current process environment only, run `Initialize-RenderTestEnvironment.ps1` twice to prove idempotence, clear the environment variable, then change the database IP allow list to private-network-only. Verify the local credential file exists with restricted ACLs and exactly `jhlee`, `twpark`, `mcchoi`, `sylee`, and `cslee`. Do not display passwords in terminal output or report files.

- [ ] **Step 6: Run live HTTPS and authorization smoke tests**

Verify `/health` and `/health/ready` return success over HTTPS; sign in as the manager and one member; complete first-password change; create fictional customer, product/product-type, price, and shipment records; confirm KRW/USD/CNY totals and percentages; restart/redeploy the Web Service; confirm data persists and users can sign in again; confirm a different member receives 403 for another member's protected data.

- [ ] **Step 7: Prepare the 담당자 test handoff**

Write `deploy/render-test-checklist.md` with the HTTPS URL, fictional-data-only warning, first-login instructions, per-module test cases, feedback fields, expected wake-up delay, and final test deadline. Deliver each password individually outside the shared document; never place all credentials in a message or shared file.

- [ ] **Step 8: Record verification and teardown procedure**

In `deploy/render-test-report.md`, record commit SHA, image/deploy ID, HTTPS URL, health results, account IDs without passwords, smoke-test results, PostgreSQL expiry, and exact Render service/database names. In the runbook, document exporting only anonymized feedback, then deleting the Web Service, PostgreSQL database, GitHub test remote/repository if no longer needed, and the local credential file after the intranet installation is accepted.

- [ ] **Step 9: Run final regression and commit documentation**

Repeat all local automated suites from Step 1 and recheck live readiness.

Expected: all local suites PASS, live readiness is healthy, and the report contains no secret.

```powershell
git add deploy docs
git commit -m "docs: hand off temporary sales test environment"
```
