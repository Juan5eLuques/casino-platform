CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Brands" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Locale" character varying(10) NOT NULL,
    "Domain" character varying(255),
    "AdminDomain" character varying(255),
    "CorsOrigins" text NOT NULL,
    "Theme" jsonb,
    "Settings" jsonb,
    "Status" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_Brands" PRIMARY KEY ("Id")
);

CREATE TABLE "Games" (
    "Id" uuid NOT NULL,
    "Code" character varying(100) NOT NULL,
    "Provider" character varying(100) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Enabled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_Games" PRIMARY KEY ("Id")
);

CREATE TABLE "ProviderAudits" (
    "Id" uuid NOT NULL,
    "Provider" character varying(100) NOT NULL,
    "Action" character varying(100) NOT NULL,
    "SessionId" text,
    "PlayerId" text,
    "RoundId" text,
    "ExternalRef" text,
    "RequestData" jsonb,
    "ResponseData" jsonb,
    "StatusCode" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_ProviderAudits" PRIMARY KEY ("Id")
);

CREATE TABLE "BackofficeUsers" (
    "Id" uuid NOT NULL,
    "BrandId" uuid,
    "Username" character varying(100) NOT NULL,
    "PasswordHash" text NOT NULL,
    "Role" text NOT NULL,
    "Status" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "LastLoginAt" timestamp with time zone,
    "ParentCashierId" uuid,
    "CommissionRate" numeric(5,2) NOT NULL DEFAULT 0.0,
    CONSTRAINT "PK_BackofficeUsers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BackofficeUsers_BackofficeUsers_ParentCashierId" FOREIGN KEY ("ParentCashierId") REFERENCES "BackofficeUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_BackofficeUsers_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id")
);

CREATE TABLE "BrandProviderConfigs" (
    "BrandId" uuid NOT NULL,
    "ProviderCode" character varying(50) NOT NULL,
    "Secret" character varying(500) NOT NULL,
    "AllowNegativeOnRollback" boolean NOT NULL,
    "Meta" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_BrandProviderConfigs" PRIMARY KEY ("BrandId", "ProviderCode"),
    CONSTRAINT "FK_BrandProviderConfigs_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Players" (
    "Id" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "ExternalId" text,
    "Username" character varying(100) NOT NULL,
    "Email" character varying(255),
    "Status" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_Players" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Players_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE
);

CREATE TABLE "BrandGames" (
    "BrandId" uuid NOT NULL,
    "GameId" uuid NOT NULL,
    "Enabled" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "Tags" text NOT NULL,
    CONSTRAINT "PK_BrandGames" PRIMARY KEY ("BrandId", "GameId"),
    CONSTRAINT "FK_BrandGames_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BrandGames_Games_GameId" FOREIGN KEY ("GameId") REFERENCES "Games" ("Id") ON DELETE CASCADE
);

CREATE TABLE "BackofficeAudits" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Action" character varying(100) NOT NULL,
    "TargetType" character varying(100) NOT NULL,
    "TargetId" character varying(255) NOT NULL,
    "Meta" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_BackofficeAudits" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BackofficeAudits_BackofficeUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CashierPlayers" (
    "CashierId" uuid NOT NULL,
    "PlayerId" uuid NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_CashierPlayers" PRIMARY KEY ("CashierId", "PlayerId"),
    CONSTRAINT "FK_CashierPlayers_BackofficeUsers_CashierId" FOREIGN KEY ("CashierId") REFERENCES "BackofficeUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CashierPlayers_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE
);

CREATE TABLE "GameSessions" (
    "Id" uuid NOT NULL,
    "PlayerId" uuid NOT NULL,
    "GameCode" character varying(100) NOT NULL,
    "Provider" character varying(100) NOT NULL,
    "Status" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "GameId" uuid,
    CONSTRAINT "PK_GameSessions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GameSessions_Games_GameId" FOREIGN KEY ("GameId") REFERENCES "Games" ("Id"),
    CONSTRAINT "FK_GameSessions_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Wallets" (
    "PlayerId" uuid NOT NULL,
    "BalanceBigint" bigint NOT NULL DEFAULT 0,
    CONSTRAINT "PK_Wallets" PRIMARY KEY ("PlayerId"),
    CONSTRAINT "FK_Wallets_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Rounds" (
    "Id" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "Status" text NOT NULL,
    "TotalBetBigint" bigint NOT NULL,
    "TotalWinBigint" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "ClosedAt" timestamp with time zone,
    CONSTRAINT "PK_Rounds" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Rounds_GameSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "GameSessions" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Ledger" (
    "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
    "BrandId" uuid NOT NULL,
    "PlayerId" uuid NOT NULL,
    "DeltaBigint" bigint NOT NULL,
    "Reason" text NOT NULL,
    "RoundId" uuid,
    "GameCode" character varying(100),
    "Provider" character varying(100),
    "ExternalRef" character varying(255),
    "Meta" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_Ledger" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Ledger_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Ledger_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Ledger_Rounds_RoundId" FOREIGN KEY ("RoundId") REFERENCES "Rounds" ("Id")
);

CREATE INDEX "IX_BackofficeAudits_UserId" ON "BackofficeAudits" ("UserId");

CREATE INDEX "IX_BackofficeUsers_BrandId" ON "BackofficeUsers" ("BrandId");

CREATE INDEX "IX_BackofficeUsers_ParentCashierId" ON "BackofficeUsers" ("ParentCashierId");

CREATE UNIQUE INDEX "IX_BackofficeUsers_Username" ON "BackofficeUsers" ("Username");

CREATE INDEX "IX_BrandGames_GameId" ON "BrandGames" ("GameId");

CREATE UNIQUE INDEX "IX_Brands_AdminDomain" ON "Brands" ("AdminDomain") WHERE "AdminDomain" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Brands_Code" ON "Brands" ("Code");

CREATE UNIQUE INDEX "IX_Brands_Domain" ON "Brands" ("Domain") WHERE "Domain" IS NOT NULL;

CREATE INDEX "IX_CashierPlayers_PlayerId" ON "CashierPlayers" ("PlayerId");

CREATE UNIQUE INDEX "IX_Games_Code" ON "Games" ("Code");

CREATE INDEX "IX_GameSessions_GameId" ON "GameSessions" ("GameId");

CREATE INDEX "IX_GameSessions_PlayerId" ON "GameSessions" ("PlayerId");

CREATE INDEX "IX_Ledger_BrandId" ON "Ledger" ("BrandId");

CREATE UNIQUE INDEX "IX_Ledger_ExternalRef" ON "Ledger" ("ExternalRef") WHERE "ExternalRef" IS NOT NULL;

CREATE INDEX "IX_Ledger_PlayerId_Id_Desc" ON "Ledger" ("PlayerId", "Id");

CREATE INDEX "IX_Ledger_RoundId" ON "Ledger" ("RoundId");

CREATE UNIQUE INDEX "IX_Players_BrandId_ExternalId" ON "Players" ("BrandId", "ExternalId") WHERE "ExternalId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Players_BrandId_Username" ON "Players" ("BrandId", "Username");

CREATE INDEX "IX_Rounds_SessionId" ON "Rounds" ("SessionId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251007235626_InitialBrandOnlySystem', '9.0.9');

ALTER TABLE "Players" ADD "CreatedByCashierId" uuid;

CREATE INDEX "IX_Players_CreatedByCashierId" ON "Players" ("CreatedByCashierId");

ALTER TABLE "Players" ADD CONSTRAINT "FK_Players_BackofficeUsers_CreatedByCashierId" FOREIGN KEY ("CreatedByCashierId") REFERENCES "BackofficeUsers" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251008024201_AddCreatedByCashierIdToPlayer', '9.0.9');

ALTER TABLE "Players" DROP CONSTRAINT "FK_Players_BackofficeUsers_CreatedByCashierId";

ALTER TABLE "Players" RENAME COLUMN "CreatedByCashierId" TO "CreatedByUserId";

ALTER INDEX "IX_Players_CreatedByCashierId" RENAME TO "IX_Players_CreatedByUserId";

ALTER TABLE "BackofficeUsers" ADD "CreatedByUserId" uuid;

CREATE INDEX "IX_BackofficeUsers_CreatedByUserId" ON "BackofficeUsers" ("CreatedByUserId");

ALTER TABLE "BackofficeUsers" ADD CONSTRAINT "FK_BackofficeUsers_BackofficeUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE SET NULL;

ALTER TABLE "Players" ADD CONSTRAINT "FK_Players_BackofficeUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251008030451_AddCreatedByUserIdToAllUsers', '9.0.9');

ALTER TABLE "Players" ADD "WalletBalance" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "BackofficeUsers" ADD "WalletBalance" numeric(18,2) NOT NULL DEFAULT 0.0;

CREATE TABLE "WalletTransactions" (
    "Id" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "FromUserId" uuid,
    "FromUserType" character varying(20),
    "ToUserId" uuid NOT NULL,
    "ToUserType" character varying(20) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Description" character varying(500),
    "CreatedByUserId" uuid NOT NULL,
    "CreatedByRole" character varying(20) NOT NULL,
    "IdempotencyKey" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_WalletTransactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WalletTransactions_BackofficeUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WalletTransactions_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE RESTRICT
);
COMMENT ON COLUMN "WalletTransactions"."FromUserType" IS 'BACKOFFICE or PLAYER';
COMMENT ON COLUMN "WalletTransactions"."ToUserType" IS 'BACKOFFICE or PLAYER';
COMMENT ON COLUMN "WalletTransactions"."Amount" IS 'Always positive amount';
COMMENT ON COLUMN "WalletTransactions"."CreatedByRole" IS 'Actor role';
COMMENT ON COLUMN "WalletTransactions"."IdempotencyKey" IS 'Unique key for idempotency';

CREATE INDEX "IX_WalletTransactions_BrandId" ON "WalletTransactions" ("BrandId");

CREATE INDEX "IX_WalletTransactions_CreatedAt" ON "WalletTransactions" ("CreatedAt");

CREATE INDEX "IX_WalletTransactions_CreatedByUserId" ON "WalletTransactions" ("CreatedByUserId");

CREATE INDEX "IX_WalletTransactions_FromUserId" ON "WalletTransactions" ("FromUserId");

CREATE UNIQUE INDEX "IX_WalletTransactions_IdempotencyKey" ON "WalletTransactions" ("IdempotencyKey");

CREATE INDEX "IX_WalletTransactions_ToUserId" ON "WalletTransactions" ("ToUserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251009062853_AddWalletTransactionTable', '9.0.9');

ALTER TABLE "BackofficeUsers" RENAME COLUMN "CommissionRate" TO "CommissionPercent";

ALTER TABLE "WalletTransactions" ADD "NewBalanceFrom" numeric(18,2);
COMMENT ON COLUMN "WalletTransactions"."NewBalanceFrom" IS 'Balance of sender AFTER transaction (null for MINT)';

ALTER TABLE "WalletTransactions" ADD "NewBalanceTo" numeric(18,2) NOT NULL DEFAULT 0.0;
COMMENT ON COLUMN "WalletTransactions"."NewBalanceTo" IS 'Balance of receiver AFTER transaction';

ALTER TABLE "WalletTransactions" ADD "PreviousBalanceFrom" numeric(18,2);
COMMENT ON COLUMN "WalletTransactions"."PreviousBalanceFrom" IS 'Balance of sender BEFORE transaction (null for MINT)';

ALTER TABLE "WalletTransactions" ADD "PreviousBalanceTo" numeric(18,2) NOT NULL DEFAULT 0.0;
COMMENT ON COLUMN "WalletTransactions"."PreviousBalanceTo" IS 'Balance of receiver BEFORE transaction';

ALTER TABLE "Players" ADD "CreatedByRole" character varying(50);

ALTER TABLE "BackofficeUsers" ADD "CreatedByRole" character varying(50);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251009173423_AddAuditFieldsToUsersAndTransactions', '9.0.9');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251009181230_AddCreatorInfoAndAuditFields', '9.0.9');

ALTER TABLE "WalletTransactions" ADD "TransactionType" character varying(20);
COMMENT ON COLUMN "WalletTransactions"."TransactionType" IS 'Transaction type: DEPOSIT, WITHDRAWAL, TRANSFER, BONUS, MINT, BURN, BET, WIN, ROLLBACK, ADJUSTMENT';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251013195658_AddTransactionTypeToWalletTransaction', '9.0.9');

ALTER TABLE "WalletTransactions" DROP COLUMN "TransactionType";

ALTER TABLE "WalletTransactions" ADD "TransactionType" integer;
COMMENT ON COLUMN "WalletTransactions"."TransactionType" IS 'Transaction type enum: 0=MINT, 1=TRANSFER, 2=BET, 3=WIN, 4=ROLLBACK, 5=DEPOSIT, 6=WITHDRAWAL, 7=BONUS, 8=ADJUSTMENT';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251013204642_AddTransactionTypeToWalletTransactions', '9.0.9');

ALTER TABLE "WalletTransactions" ALTER COLUMN "PreviousBalanceTo" DROP NOT NULL;

ALTER TABLE "WalletTransactions" ALTER COLUMN "NewBalanceTo" DROP NOT NULL;

ALTER TABLE "WalletTransactions" ADD "ActorIp" character varying(45);

ALTER TABLE "WalletTransactions" ADD "ApprovedAt" timestamp with time zone;

ALTER TABLE "WalletTransactions" ADD "ApprovedByUserId" uuid;

ALTER TABLE "WalletTransactions" ADD "Metadata" jsonb;

ALTER TABLE "WalletTransactions" ADD "Notes" character varying(1000);

ALTER TABLE "BackofficeUsers" ADD "HierarchyLevel" integer NOT NULL DEFAULT 0;

ALTER TABLE "BackofficeUsers" ADD "HierarchyPath" character varying(500);

ALTER TABLE "BackofficeUsers" ADD "ParentAdminId" uuid;

CREATE TABLE "CommissionAccruals" (
    "Id" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ParentUserId" uuid,
    "PeriodMonth" integer NOT NULL,
    "PeriodYear" integer NOT NULL,
    "BaseAmount" bigint NOT NULL,
    "CommissionRate" numeric(5,4) NOT NULL,
    "CommissionAmount" bigint NOT NULL,
    "Settled" boolean NOT NULL,
    "SettledAt" timestamp with time zone,
    "SettledTransactionId" uuid,
    "SourceType" character varying(50),
    "SourceTransactionId" uuid,
    "SourceRoundId" uuid,
    "SourcePlayerId" uuid,
    "Notes" character varying(1000),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_CommissionAccruals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommissionAccruals_BackofficeUsers_ParentUserId" FOREIGN KEY ("ParentUserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_CommissionAccruals_BackofficeUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommissionAccruals_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommissionAccruals_Players_SourcePlayerId" FOREIGN KEY ("SourcePlayerId") REFERENCES "Players" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_CommissionAccruals_Rounds_SourceRoundId" FOREIGN KEY ("SourceRoundId") REFERENCES "Rounds" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_CommissionAccruals_WalletTransactions_SettledTransactionId" FOREIGN KEY ("SettledTransactionId") REFERENCES "WalletTransactions" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_CommissionAccruals_WalletTransactions_SourceTransactionId" FOREIGN KEY ("SourceTransactionId") REFERENCES "WalletTransactions" ("Id") ON DELETE SET NULL
);

CREATE TABLE "MonthlyClosures" (
    "Id" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "UserId" uuid,
    "PeriodMonth" integer NOT NULL,
    "PeriodYear" integer NOT NULL,
    "TotalHandle" bigint NOT NULL,
    "TotalPayouts" bigint NOT NULL,
    "GrossGamingRevenue" bigint NOT NULL,
    "TotalCommissionsPaid" bigint NOT NULL,
    "ClosureStatus" character varying(50) NOT NULL DEFAULT 'PENDING',
    "ClosedAt" timestamp with time zone,
    "ClosedByUserId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_MonthlyClosures" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MonthlyClosures_BackofficeUsers_ClosedByUserId" FOREIGN KEY ("ClosedByUserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_MonthlyClosures_BackofficeUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_MonthlyClosures_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_WalletTransactions_ApprovedByUserId" ON "WalletTransactions" ("ApprovedByUserId");

CREATE INDEX "IX_BackofficeUsers_HierarchyPath" ON "BackofficeUsers" ("HierarchyPath");

CREATE INDEX "IX_BackofficeUsers_ParentAdminId" ON "BackofficeUsers" ("ParentAdminId");

CREATE INDEX "IX_CommissionAccruals_BrandId_PeriodYear_PeriodMonth" ON "CommissionAccruals" ("BrandId", "PeriodYear", "PeriodMonth");

CREATE UNIQUE INDEX "IX_CommissionAccruals_BrandId_UserId_PeriodYear_PeriodMonth_So~" ON "CommissionAccruals" ("BrandId", "UserId", "PeriodYear", "PeriodMonth", "SourceTransactionId", "SourceRoundId");

CREATE INDEX "IX_CommissionAccruals_ParentUserId" ON "CommissionAccruals" ("ParentUserId");

CREATE INDEX "IX_CommissionAccruals_Settled" ON "CommissionAccruals" ("Settled") WHERE "Settled" = false;

CREATE INDEX "IX_CommissionAccruals_SettledTransactionId" ON "CommissionAccruals" ("SettledTransactionId");

CREATE INDEX "IX_CommissionAccruals_SourcePlayerId" ON "CommissionAccruals" ("SourcePlayerId");

CREATE INDEX "IX_CommissionAccruals_SourceRoundId" ON "CommissionAccruals" ("SourceRoundId") WHERE "SourceRoundId" IS NOT NULL;

CREATE INDEX "IX_CommissionAccruals_SourceTransactionId" ON "CommissionAccruals" ("SourceTransactionId") WHERE "SourceTransactionId" IS NOT NULL;

CREATE INDEX "IX_CommissionAccruals_UserId_PeriodYear_PeriodMonth" ON "CommissionAccruals" ("UserId", "PeriodYear", "PeriodMonth");

CREATE INDEX "IX_MonthlyClosures_BrandId_PeriodYear_PeriodMonth" ON "MonthlyClosures" ("BrandId", "PeriodYear", "PeriodMonth");

CREATE UNIQUE INDEX "IX_MonthlyClosures_BrandId_UserId_PeriodYear_PeriodMonth" ON "MonthlyClosures" ("BrandId", "UserId", "PeriodYear", "PeriodMonth");

CREATE INDEX "IX_MonthlyClosures_ClosedByUserId" ON "MonthlyClosures" ("ClosedByUserId");

CREATE INDEX "IX_MonthlyClosures_ClosureStatus" ON "MonthlyClosures" ("ClosureStatus") WHERE "ClosureStatus" != 'COMPLETED';

CREATE INDEX "IX_MonthlyClosures_UserId_PeriodYear_PeriodMonth" ON "MonthlyClosures" ("UserId", "PeriodYear", "PeriodMonth") WHERE "UserId" IS NOT NULL;

ALTER TABLE "BackofficeUsers" ADD CONSTRAINT "FK_BackofficeUsers_BackofficeUsers_ParentAdminId" FOREIGN KEY ("ParentAdminId") REFERENCES "BackofficeUsers" ("Id") ON DELETE RESTRICT;

ALTER TABLE "WalletTransactions" ADD CONSTRAINT "FK_WalletTransactions_BackofficeUsers_ApprovedByUserId" FOREIGN KEY ("ApprovedByUserId") REFERENCES "BackofficeUsers" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251022013220_AddMultilevelHierarchyAndCommissions', '9.0.9');

ALTER TABLE "Games" ADD "AdditionalTags" text NOT NULL DEFAULT '';

ALTER TABLE "Games" ADD "Category" character varying(50);

ALTER TABLE "Games" ADD "ImageUrl" character varying(500);

ALTER TABLE "Games" ADD "IsFeatured" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Games" ADD "IsNew" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Games" ADD "LaunchId" character varying(200);

ALTER TABLE "Games" ADD "MaxBet" numeric(18,2);

ALTER TABLE "Games" ADD "MinBet" numeric(18,2);

ALTER TABLE "Games" ADD "ProviderId" uuid;

ALTER TABLE "Games" ADD "RTP" numeric(5,2);

ALTER TABLE "Games" ADD "UpdatedAt" timestamp with time zone;

ALTER TABLE "Games" ADD "Volatility" character varying(20);

CREATE TABLE "GameLaunchLogs" (
    "Id" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "PlayerId" uuid NOT NULL,
    "GameId" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "Provider" character varying(50) NOT NULL,
    "LaunchUrl" text NOT NULL,
    "SessionToken" character varying(500) NOT NULL,
    "Success" boolean NOT NULL,
    "ErrorMessage" character varying(1000),
    "IpAddress" character varying(45),
    "UserAgent" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_GameLaunchLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GameLaunchLogs_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_GameLaunchLogs_GameSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "GameSessions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_GameLaunchLogs_Games_GameId" FOREIGN KEY ("GameId") REFERENCES "Games" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_GameLaunchLogs_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "GameProviders" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "LaunchEndpointTemplate" text NOT NULL,
    "RequiresSessionToken" boolean NOT NULL,
    "SupportsRealMode" boolean NOT NULL,
    "SupportsDemoMode" boolean NOT NULL,
    "DefaultMeta" jsonb,
    "Enabled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_GameProviders" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_Games_ProviderId" ON "Games" ("ProviderId");

CREATE INDEX "IX_GameLaunchLogs_BrandId" ON "GameLaunchLogs" ("BrandId");

CREATE INDEX "IX_GameLaunchLogs_CreatedAt" ON "GameLaunchLogs" ("CreatedAt");

CREATE INDEX "IX_GameLaunchLogs_GameId" ON "GameLaunchLogs" ("GameId");

CREATE INDEX "IX_GameLaunchLogs_PlayerId" ON "GameLaunchLogs" ("PlayerId");

CREATE INDEX "IX_GameLaunchLogs_Provider_CreatedAt" ON "GameLaunchLogs" ("Provider", "CreatedAt");

CREATE INDEX "IX_GameLaunchLogs_SessionId" ON "GameLaunchLogs" ("SessionId");

CREATE UNIQUE INDEX "IX_GameProviders_Code" ON "GameProviders" ("Code");

ALTER TABLE "Games" ADD CONSTRAINT "FK_Games_GameProviders_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "GameProviders" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251023050852_GameCatalogSystem', '9.0.9');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251023050859_AddGameCatalogAndLaunchSystem', '9.0.9');

ALTER TABLE "Games" ADD "Type" character varying(20) NOT NULL DEFAULT 'SLOT';

CREATE INDEX "IX_Games_Type" ON "Games" ("Type");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251023052805_AddGameTypeField', '9.0.9');

CREATE TABLE "BrandSettings" (
    "Id" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "Colors" jsonb NOT NULL,
    "Images" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_BrandSettings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BrandSettings_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_BrandSettings_BrandId" ON "BrandSettings" ("BrandId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251113053433_AddBrandSettingsTable', '9.0.9');

COMMIT;

