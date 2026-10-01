CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE daily_rates (
        "Id" uuid NOT NULL,
        "Date" date NOT NULL,
        "Currency" text NOT NULL,
        "Rate" numeric(20,8) NOT NULL,
        "FetchedAt" timestamptz NOT NULL,
        "FetchStatus" text NOT NULL,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_daily_rates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE products (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "ItemType" text NOT NULL,
        "Specification" text NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_products" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_products_item_type" CHECK ("ItemType" IN ('Product','Goods'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE users (
        "Id" uuid NOT NULL,
        "LoginId" character varying(100) NOT NULL,
        "PasswordHash" character varying(500) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Email" character varying(320) NOT NULL,
        "Role" text NOT NULL,
        "Status" text NOT NULL,
        "PasswordChangeRequired" boolean NOT NULL,
        "LockedUntil" timestamp with time zone,
        "FailedLoginAttempts" integer NOT NULL,
        "LastActivityAt" timestamp with time zone,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE audit_entries (
        "Id" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "Target" character varying(200) NOT NULL,
        "Action" character varying(100) NOT NULL,
        "BeforeJson" jsonb,
        "AfterJson" jsonb,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_audit_entries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_audit_entries_users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE customers (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "CompanyName" character varying(200) NOT NULL,
        "Country" character varying(100) NOT NULL,
        "DefaultCurrency" text NOT NULL,
        "OwnerUserId" uuid NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_customers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_customers_users_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE import_batches (
        "Id" uuid NOT NULL,
        "Kind" text NOT NULL,
        "FileName" character varying(260) NOT NULL,
        "RequestedBy" uuid NOT NULL,
        "Status" text NOT NULL,
        "NewCount" integer NOT NULL,
        "DuplicateCount" integer NOT NULL,
        "ChangeCandidateCount" integer NOT NULL,
        "ErrorCount" integer NOT NULL,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_import_batches" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_import_batches_users_RequestedBy" FOREIGN KEY ("RequestedBy") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE monthly_rates (
        "Id" uuid NOT NULL,
        "YearMonth" character varying(7) NOT NULL,
        "Currency" text NOT NULL,
        "Rate" numeric(20,8) NOT NULL,
        "UpdatedBy" uuid NOT NULL,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_monthly_rates" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_monthly_rates_users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE sales_targets (
        "Id" uuid NOT NULL,
        "YearMonth" character varying(7) NOT NULL,
        "UserId" uuid NOT NULL,
        "TargetKrw" numeric(20,4) NOT NULL,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_sales_targets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_sales_targets_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE prices (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Currency" text NOT NULL,
        "UnitPrice" numeric(20,4) NOT NULL,
        "ValidFrom" date NOT NULL,
        "ValidTo" date,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_prices" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_prices_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customers ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_prices_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE TABLE shipments (
        "Id" uuid NOT NULL,
        "ShipmentNumber" character varying(100),
        "ShipmentDate" date NOT NULL,
        "CustomerId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Quantity" numeric(20,4) NOT NULL,
        "UnitPrice" numeric(20,4) NOT NULL,
        "Currency" text NOT NULL,
        "SupplyAmount" numeric(20,4) NOT NULL,
        "AppliedRate" numeric(20,8) NOT NULL,
        "KrwAmount" numeric(20,4) NOT NULL,
        "OwnerUserId" uuid NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamptz NOT NULL,
        "UpdatedAt" timestamptz NOT NULL,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_shipments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_shipments_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customers ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_shipments_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_shipments_users_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE INDEX "IX_audit_entries_ActorUserId" ON audit_entries ("ActorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_customers_Code" ON customers ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE INDEX "IX_customers_OwnerUserId" ON customers ("OwnerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_daily_rates_Date_Currency" ON daily_rates ("Date", "Currency");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE INDEX "IX_import_batches_RequestedBy" ON import_batches ("RequestedBy");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE INDEX "IX_monthly_rates_UpdatedBy" ON monthly_rates ("UpdatedBy");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_monthly_rates_YearMonth_Currency" ON monthly_rates ("YearMonth", "Currency");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE INDEX "IX_prices_CustomerId_ProductId_ValidFrom" ON prices ("CustomerId", "ProductId", "ValidFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE INDEX "IX_prices_ProductId" ON prices ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS btree_gist;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    ALTER TABLE prices ADD CONSTRAINT "EX_prices_no_overlap"
    EXCLUDE USING gist (
        "CustomerId" WITH =,
        "ProductId" WITH =,
        daterange("ValidFrom", COALESCE("ValidTo", 'infinity'::date), '[]') WITH &&
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_products_Code" ON products ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_sales_targets_UserId_YearMonth" ON sales_targets ("UserId", "YearMonth");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_shipments_CustomerId_ShipmentNumber" ON shipments ("CustomerId", "ShipmentNumber") WHERE "ShipmentNumber" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE INDEX "IX_shipments_OwnerUserId" ON shipments ("OwnerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE INDEX "IX_shipments_ProductId" ON shipments ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_LoginId" ON users ("LoginId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001081842_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261001081842_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

