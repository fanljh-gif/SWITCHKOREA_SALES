# Local Intranet Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Install the SWITCH KOREA sales system on PC `JHLEE` so five named users can securely sign in from their own intranet PCs, persist data in PostgreSQL, and recover it from daily 30-day backups.

**Architecture:** IIS serves the React bundle and ASP.NET Core API from `https://sales.switchkorea.local`; the API alone connects to a localhost-only PostgreSQL Windows service. A dedicated setup executable performs migrations and idempotent account seeding, while PowerShell scripts orchestrate prerequisites, certificates, IIS, firewall, scheduled backups, and client trust material.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10.0.12, Npgsql 10.0.3, PostgreSQL 18 Windows service, IIS, PowerShell 5.1-compatible scripts, Windows Certificate Store, Windows Task Scheduler, React/Vite

**Spec:** `docs/superpowers/specs/2026-10-02-local-intranet-deployment-design.md`

## Global Constraints

- Server PC is `JHLEE`; canonical URL is `https://sales.switchkorea.local`.
- PostgreSQL listens on localhost only; no inbound PostgreSQL firewall rule may be created.
- IIS exposes HTTPS 443 only to the explicitly supplied intranet CIDR and redirects HTTP to HTTPS.
- Install root is `C:\SwitchKorea\SalesManagement`; backups are in `C:\SwitchKorea\Backups` at 02:00 with 30-day retention.
- Initial credentials are written only to `C:\SwitchKorea\Secrets\initial-credentials.txt`, ACL-restricted to the invoking user and local Administrators.
- Root and server certificate private keys remain in the server LocalMachine certificate store; clients receive only `SwitchKorea-RootCA.cer`.
- No plaintext database password, connection string, or initial password may enter Git or application/install logs.
- Initial account seeding is idempotent and never resets an existing password.
- Tests and validations run without per-command approval, per the user's standing instruction; Windows UAC elevation remains an unavoidable OS security prompt.
- The user must still approve the completed installation stage before it is declared operational.

## Review Focus

- Re-running setup after a partial failure must resume without replacing existing accounts, certificates, database data, or passwords; covered by Tasks 1, 2, and 4.
- A dynamic or ambiguous server IPv4 address must stop installation before DNS/hosts or firewall configuration; covered by Task 4.
- Missing, expired, or incorrectly scoped certificates must fail HTTPS readiness rather than silently falling back to HTTP; covered by Tasks 3 and 5.
- Backup success with an empty, truncated, or unrestorable dump must not be accepted; covered by Task 5.
- Production startup without a protected PostgreSQL connection or persistent data-protection key directory must fail readiness; covered by Task 2.

---

### Task 1: Idempotent Initial Account Seeder

**Files:**
- Create: `src/backend/SalesManagement.Setup/SalesManagement.Setup.csproj`
- Create: `src/backend/SalesManagement.Setup/InitialAccountDefinition.cs`
- Create: `src/backend/SalesManagement.Setup/InitialAccountSeeder.cs`
- Create: `src/backend/SalesManagement.Setup/InitialCredentialWriter.cs`
- Create: `src/backend/SalesManagement.Setup/Program.cs`
- Create: `src/backend/SalesManagement.Tests/InitialAccountSeederTests.cs`
- Modify: `src/backend/SalesManagement.sln`

**Interfaces:**
- Consumes: `SalesDbContext`, `IUserRepository`, `IPasswordHasher<User>`, `ConnectionStrings__SalesManagement`.
- Produces: `InitialAccountSeeder.SeedAsync(IReadOnlyList<InitialAccountDefinition>, CancellationToken): Task<SeedResult>` and a setup CLI accepting `--credentials-path` and `--migrate`.

- [ ] **Step 1: Write failing account-seeding tests**

Assert exact IDs, names, roles, and emails for `jhlee`, `twpark`, `mcchoi`, `sylee`, and `cslee`; assert all new users require password change; assert generated passwords are distinct and at least 16 characters; assert a second run creates zero users and preserves the stored password hashes.

- [ ] **Step 2: Run the focused tests and verify RED**

Run: `dotnet test src/backend/SalesManagement.Tests --filter InitialAccountSeederTests`

Expected: FAIL because `SalesManagement.Setup` and `InitialAccountSeeder` do not exist.

- [ ] **Step 3: Implement account definitions and seeding**

Define `InitialAccountDefinition(string LoginId, string Name, string Email, Role Role)`, `InitialCredential(string LoginId, string TemporaryPassword)`, and `SeedResult(IReadOnlyList<InitialCredential> Created, int ExistingCount)`. Use `RandomNumberGenerator` for passwords, `PasswordHasher<User>` for storage, one DB transaction, and `PasswordChangeRequired = true`; query by login ID before every insert.

- [ ] **Step 4: Implement the setup CLI and protected credential writer**

`Program.cs` requires `ConnectionStrings__SalesManagement`, optionally runs `Database.MigrateAsync`, seeds the five approved accounts, and writes only newly created credentials. `InitialCredentialWriter.Write(string path, IReadOnlyList<InitialCredential> credentials): void` must reject paths outside `C:\SwitchKorea\Secrets`, create the directory, remove inherited ACLs, and grant read access only to the invoking user and local Administrators; stdout reports counts and path but never passwords.

- [ ] **Step 5: Run focused and regression tests**

Run: `dotnet test src/backend/SalesManagement.Tests --filter InitialAccountSeederTests`

Expected: PASS.

Run: `dotnet test src/backend/SalesManagement.sln`

Expected: all existing and new tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/backend
git commit -m "feat: add secure initial account bootstrap"
```

### Task 2: Production PostgreSQL and Persistent Authentication Runtime

**Files:**
- Create: `src/backend/SalesManagement.Api/Configuration/ProductionRuntimeOptions.cs`
- Create: `src/backend/SalesManagement.Api/Configuration/ProductionRuntimeExtensions.cs`
- Create: `src/backend/SalesManagement.Tests/ProductionRuntimeTests.cs`
- Modify: `src/backend/SalesManagement.Api/Program.cs`
- Modify: `src/backend/SalesManagement.Api/SalesManagement.Api.csproj`
- Modify: `src/backend/SalesManagement.Api/appsettings.json`

**Interfaces:**
- Consumes: `ConnectionStrings__SalesManagement`, `SWITCHKOREA_DATA_PROTECTION_PATH`, IIS environment, Task 1 migration output.
- Produces: `AddSalesProductionRuntime(IServiceCollection, IConfiguration, IWebHostEnvironment)` and database-aware `/health/ready`.

- [ ] **Step 1: Write failing production-runtime tests**

Test that Production without a connection string throws a configuration exception, Development may use in-memory repositories, readiness fails when PostgreSQL is unreachable, and data-protection keys persist to the configured directory rather than an ephemeral store.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test src/backend/SalesManagement.Tests --filter ProductionRuntimeTests`

Expected: FAIL because production runtime configuration is not separated and `/health/ready` does not check PostgreSQL.

- [ ] **Step 3: Implement strict environment selection**

Production must require PostgreSQL and `SWITCHKOREA_DATA_PROTECTION_PATH`; Development alone may fall back to in-memory repositories. Persist ASP.NET Core data-protection keys under `C:\SwitchKorea\DataProtectionKeys` and protect them with Windows DPAPI at LocalMachine scope.

- [ ] **Step 4: Implement HTTPS and readiness behavior**

Enable HSTS, HTTPS redirection, secure cookies, IIS integration, and `/health/ready` with `SalesDbContext.Database.CanConnectAsync`. Keep `/health` as a liveness endpoint that does not disclose configuration details.

- [ ] **Step 5: Run focused tests, full tests, and Release publish**

Run: `dotnet test src/backend/SalesManagement.Tests --filter ProductionRuntimeTests`

Run: `dotnet test src/backend/SalesManagement.sln`

Run: `dotnet publish src/backend/SalesManagement.Api/SalesManagement.Api.csproj -c Release -o artifacts/publish`

Expected: all tests PASS; publish completes with zero errors; no source/config file contains a connection-string password.

- [ ] **Step 6: Commit**

```bash
git add src/backend
git commit -m "feat: harden production database and authentication runtime"
```

### Task 3: Certificate, IIS, Firewall, and Client Trust Automation

**Files:**
- Create: `deploy/scripts/New-SwitchKoreaCertificate.ps1`
- Create: `deploy/scripts/Install-SwitchKoreaClientTrust.ps1`
- Create: `deploy/scripts/Test-LocalDeploymentPrerequisites.ps1`
- Create: `deploy/scripts/Test-DeploymentScripts.ps1`
- Modify: `deploy/scripts/Install-SalesManagement.ps1`
- Modify: `deploy/iis/web.config`
- Modify: `deploy/runbook.md`

**Interfaces:**
- Consumes: `ServerIp`, `IntranetCidr`, `sales.switchkorea.local`, LocalMachine certificate store, published artifact.
- Produces: server certificate thumbprint, `C:\SwitchKorea\Certificates\SwitchKorea-RootCA.cer`, IIS HTTPS site, CIDR-scoped firewall rule, and a client trust installer.

- [ ] **Step 1: Write the deployment-script RED checks**

`Test-DeploymentScripts.ps1` must parse every script and assert: missing/ambiguous IPv4 stops prerequisite validation; root public export contains no private key; install plan opens only ports 80/443 and never 5432; `-PlanOnly` changes no IIS, certificate, firewall, or hosts state; client trust targets `Cert:\LocalMachine\Root` and requires elevation.

- [ ] **Step 2: Run the script checks and verify RED**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/scripts/Test-DeploymentScripts.ps1`

Expected: FAIL because the new prerequisite, certificate, and client trust scripts do not exist.

- [ ] **Step 3: Implement prerequisite validation**

`Test-LocalDeploymentPrerequisites.ps1 -ServerIp 192.0.2.10 -IntranetCidr 192.0.2.0/24` demonstrates the parameter contract using documentation-only addresses. The implementation validates admin rights, hostname `JHLEE`, non-loopback IPv4, CIDR syntax, disk space, supported Windows/IIS features, existing port bindings, and restart-pending state; it returns a structured result and exits nonzero on a blocking condition.

- [ ] **Step 4: Implement certificate creation and client trust**

Create an exportable public root certificate and a non-exportable server private key for DNS SAN `sales.switchkorea.local` and IP SAN `ServerIp`. Reuse valid matching certificates; never replace them silently. Export only the root `.cer`. The client script installs that `.cer` into LocalMachine Root and verifies the thumbprint.

- [ ] **Step 5: Update IIS and firewall installation**

Make `Install-SalesManagement.ps1` PowerShell 5.1-compatible and idempotent. Require `ServerIp`, `IntranetCidr`, install root, and certificate thumbprint; enable IIS components, deploy the combined publish output, bind HTTPS 443, redirect HTTP, set AlwaysRunning, and create only CIDR-scoped 80/443 rules. Add `-PlanOnly` output with secrets masked.

- [ ] **Step 6: Run script checks and source security scan**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/scripts/Test-DeploymentScripts.ps1`

Run: `rg -n -i "password\\s*=|connectionstrings.*password" src deploy`

Expected: tests PASS; scan finds no committed secret assignment.

- [ ] **Step 7: Commit**

```bash
git add deploy src/backend
git commit -m "feat: automate secure IIS and certificate deployment"
```

### Task 4: Local PostgreSQL Provisioning and Resumable Installer

**Files:**
- Create: `deploy/scripts/Install-LocalPostgres.ps1`
- Create: `deploy/scripts/Install-LocalSalesManagement.ps1`
- Create: `deploy/scripts/DeploymentState.psm1`
- Create: `deploy/tests/LocalInstaller.Tests.ps1`
- Modify: `.gitignore`
- Modify: `deploy/runbook.md`

**Interfaces:**
- Consumes: official PostgreSQL 18 Windows installer, official .NET 10 Hosting Bundle installer, Tasks 1–3 scripts and artifacts.
- Produces: localhost-only PostgreSQL service, `switch_korea_sales`, least-privilege application login, protected Machine connection setting, and resumable state at `C:\SwitchKorea\InstallState\state.json`.

- [ ] **Step 1: Write failing installer state-machine tests**

Use `-PlanOnly` and mocked command runners to assert ordered phases `Prerequisites → PostgreSQL → Database → Migration → Accounts → Publish → Certificate → IIS → Firewall → Backup`; completed phases are skipped on rerun; a failed phase is retried; secrets are redacted; pending reboot stops after persisting state.

- [ ] **Step 2: Run installer tests and verify RED**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/LocalInstaller.Tests.ps1`

Expected: FAIL because the local installer and state module do not exist.

- [ ] **Step 3: Implement PostgreSQL provisioning**

`Install-LocalPostgres.ps1` accepts explicit verified installer path and checksum, installs PostgreSQL 18 silently when absent, configures `listen_addresses = 'localhost'`, preserves the generated admin password only in process memory, creates `switch_korea_sales` and a least-privilege application role, and records the protected Machine connection setting without logging it.

- [ ] **Step 4: Implement the resumable orchestrator**

`Install-LocalSalesManagement.ps1` calls each phase exactly once, writes only non-secret status to `state.json`, invokes the Task 1 setup CLI, and stops with a clear resume command when UAC, reboot, unavailable installers, invalid IP/CIDR, or port conflicts prevent safe progress.

- [ ] **Step 5: Run plan-mode tests and verify installer output**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/LocalInstaller.Tests.ps1`

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/scripts/Install-LocalSalesManagement.ps1 -NetworkConfigPath C:\SwitchKorea\InstallState\network.json -PlanOnly`

Expected: tests PASS; plan lists all phases, creates no external state, and contains no password.

- [ ] **Step 6: Commit**

```bash
git add .gitignore deploy
git commit -m "feat: add resumable local PostgreSQL installer"
```

### Task 5: Verified Backup, Restore, and Scheduled Task

**Files:**
- Create: `deploy/scripts/Register-SalesBackupTask.ps1`
- Create: `deploy/scripts/Test-SalesBackupRestore.ps1`
- Create: `deploy/tests/BackupRestore.Tests.ps1`
- Modify: `deploy/scripts/Backup-SalesDatabase.ps1`
- Modify: `deploy/scripts/Restore-SalesDatabase.ps1`
- Modify: `deploy/runbook.md`

**Interfaces:**
- Consumes: PostgreSQL `pg_dump`, `pg_restore`, `psql`, protected backup credential, `C:\SwitchKorea\Backups`.
- Produces: daily 02:00 scheduled task, verified custom-format dump, 30-day retention, and isolated restore-count report.

- [ ] **Step 1: Write failing backup/restore safety tests**

With fake process runners, assert that a nonzero dump exit, missing output, zero-byte output, or unreadable archive fails; retention deletes only matching dumps older than 30 days; restore requires a non-production target and explicit confirmation; count comparison covers users, customers, products, shipments, and audit entries.

- [ ] **Step 2: Run tests and verify RED**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/BackupRestore.Tests.ps1`

Expected: FAIL because verified archive and scheduled-task behavior are absent.

- [ ] **Step 3: Harden backup and restore scripts**

After `pg_dump`, run `pg_restore --list`, require a nonzero file, write a SHA-256 sidecar, and only then apply retention. Restore rejects `switch_korea_sales` unless `-AllowProductionRestore` and ShouldProcess confirmation are both present.

- [ ] **Step 4: Implement scheduled task registration and isolated verification**

Register `SwitchKorea-Sales-Backup` for 02:00 daily under a least-privilege service identity. `Test-SalesBackupRestore.ps1` creates a uniquely named verification DB, restores the newest dump, compares five table counts, reports results, and removes only that verified temporary DB.

- [ ] **Step 5: Run tests**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/BackupRestore.Tests.ps1`

Expected: PASS with no dependency on production data.

- [ ] **Step 6: Commit**

```bash
git add deploy
git commit -m "feat: add verified daily database recovery workflow"
```

### Task 6: Install on JHLEE and Prove Intranet Readiness

**Files:**
- Modify: `deploy/runbook.md`
- Create: `deploy/installation-report.md`

**Interfaces:**
- Consumes: all prior tasks, actual static server IPv4 and intranet CIDR discovered by the prerequisite script.
- Produces: running local installation, client certificate package, one-time credentials file, backup/restore evidence, and installation report.

- [ ] **Step 1: Run the complete automated regression before installation**

Run: Front-end Vitest suite and Vite build; `dotnet test src/backend/SalesManagement.sln`; deployment, installer, and backup PowerShell test suites.

Expected: every suite PASS before machine state changes.

- [ ] **Step 2: Resolve and record network inputs**

Run the prerequisite script with `-DiscoverNetwork`, select the single active static IPv4 and matching CIDR, and save them to `C:\SwitchKorea\InstallState\network.json`; stop if multiple adapters are active or the address is dynamic without a DHCP reservation. Record only IP/CIDR, not credentials, in the installation report.

- [ ] **Step 3: Execute the installer with elevation**

Run `Install-LocalSalesManagement.ps1 -NetworkConfigPath C:\SwitchKorea\InstallState\network.json` with the official installer files. Accept the Windows UAC prompt. If restart is required, reboot and rerun the identical command so state resumes safely.

- [ ] **Step 4: Verify server persistence and HTTPS**

Confirm PostgreSQL and IIS automatic start, `/health` and `/health/ready` success, certificate chain/SAN/expiry, HTTP-to-HTTPS redirect, no port 5432 listener on non-loopback interfaces, page/API availability at `https://sales.switchkorea.local`, and a power policy that does not suspend the server during business or backup hours.

- [ ] **Step 5: Verify accounts and authorization**

Confirm five database users with exact approved names/emails, unique hashes, and `PasswordChangeRequired = true`. Sign in with manager and one member credential, change their passwords, create an owned customer/price/shipment, confirm persistence after IIS recycle, and confirm a different member receives 403 for that customer.

- [ ] **Step 6: Verify backup and isolated restore**

Run the backup task on demand, validate archive and checksum, restore into a uniquely named verification database, compare required counts, and remove the verification database after recording evidence.

- [ ] **Step 7: Prepare client onboarding package**

Place the public root `.cer`, `Install-SwitchKoreaClientTrust.ps1`, hosts/DNS instructions, URL, and per-user credential distribution instructions in `C:\SwitchKorea\ClientSetup`. Do not copy server or root private keys.

- [ ] **Step 8: Write installation report and final regression**

Record component versions, service states, certificate thumbprint/expiry, URL, firewall CIDR, test counts, backup path, restore count comparison, and remaining DNS/client rollout steps. Re-run all automated suites after installation.

- [ ] **Step 9: Request final operational approval and commit**

After user approval:

```bash
git add src deploy docs
git commit -m "feat: install SWITCH KOREA intranet sales service"
```
