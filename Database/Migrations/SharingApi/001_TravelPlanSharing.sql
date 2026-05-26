USE PlaniranjePutovanja_Sharing;
GO

IF OBJECT_ID(N'dbo.TravelPlanShareRecipients', N'U') IS NOT NULL DROP TABLE dbo.TravelPlanShareRecipients;
IF OBJECT_ID(N'dbo.TravelPlanShareLinks', N'U') IS NOT NULL DROP TABLE dbo.TravelPlanShareLinks;
GO

CREATE TABLE dbo.TravelPlanShareLinks (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelPlanShareLinks PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    TokenHash VARBINARY(32) NOT NULL,
    Permission NVARCHAR(10) NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
    CreatedAtUtc DATETIME2(3) NOT NULL,
    ExpiresAtUtc DATETIME2(3) NULL,
    RevokedAtUtc DATETIME2(3) NULL
);

CREATE UNIQUE INDEX UX_TravelPlanShareLinks_TokenHash ON dbo.TravelPlanShareLinks (TokenHash);
GO

CREATE TABLE dbo.TravelPlanShareRecipients (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelPlanShareRecipients PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    RecipientUserId UNIQUEIDENTIFIER NOT NULL,
    Permission NVARCHAR(10) NOT NULL,
    ClaimedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL
);

CREATE UNIQUE INDEX UX_TravelPlanShareRecipients_Plan_User
    ON dbo.TravelPlanShareRecipients (TravelPlanId, RecipientUserId);
GO
