/*
  Deljenje planova putovanja preko tokena (QR) sa nivoima pristupa VIEW / EDIT.
  Pokreni nakon 005_TravelPlans.sql (i preporučeno nakon 009_TravelChecklistItems.sql).
*/
IF DB_NAME() <> N'PlaniranjePutovanja'
    PRINT N'Upozorenje: skripta se izvršava nad bazom [' + DB_NAME() + N'], očekivano je [PlaniranjePutovanja].';
GO

IF OBJECT_ID(N'dbo.TravelPlans', N'U') IS NULL
    THROW 50001, 'Nedostaje tabela dbo.TravelPlans. Pokreni prvo 005_TravelPlans.sql nad istom bazom.', 1;
GO

IF OBJECT_ID(N'dbo.TravelPlanShareRecipients', N'U') IS NOT NULL
    DROP TABLE dbo.TravelPlanShareRecipients;
GO

IF OBJECT_ID(N'dbo.TravelPlanShareLinks', N'U') IS NOT NULL
    DROP TABLE dbo.TravelPlanShareLinks;
GO

CREATE TABLE dbo.TravelPlanShareLinks
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelPlanShareLinks PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    TokenHash VARBINARY(32) NOT NULL,
    Permission NVARCHAR(10) NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
    CreatedAtUtc DATETIME2(3) NOT NULL,
    ExpiresAtUtc DATETIME2(3) NULL,
    RevokedAtUtc DATETIME2(3) NULL,
    CONSTRAINT FK_TravelPlanShareLinks_TravelPlans FOREIGN KEY (TravelPlanId)
        REFERENCES dbo.TravelPlans (Id) ON DELETE CASCADE,
    CONSTRAINT CK_TravelPlanShareLinks_Permission CHECK (Permission IN (N'view', N'edit')),
    CONSTRAINT UQ_TravelPlanShareLinks_TokenHash UNIQUE (TokenHash)
);
GO

CREATE NONCLUSTERED INDEX IX_TravelPlanShareLinks_TravelPlanId_CreatedAt
    ON dbo.TravelPlanShareLinks (TravelPlanId, CreatedAtUtc);
GO

CREATE TABLE dbo.TravelPlanShareRecipients
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelPlanShareRecipients PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    RecipientUserId UNIQUEIDENTIFIER NOT NULL,
    Permission NVARCHAR(10) NOT NULL,
    ClaimedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL,
    CONSTRAINT FK_TravelPlanShareRecipients_TravelPlans FOREIGN KEY (TravelPlanId)
        REFERENCES dbo.TravelPlans (Id) ON DELETE CASCADE,
    CONSTRAINT CK_TravelPlanShareRecipients_Permission CHECK (Permission IN (N'view', N'edit')),
    CONSTRAINT UQ_TravelPlanShareRecipients_Plan_User UNIQUE (TravelPlanId, RecipientUserId)
);
GO

CREATE NONCLUSTERED INDEX IX_TravelPlanShareRecipients_UserId_UpdatedAt
    ON dbo.TravelPlanShareRecipients (RecipientUserId, UpdatedAtUtc DESC);
GO
