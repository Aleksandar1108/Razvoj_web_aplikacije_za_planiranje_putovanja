/*
  Planovi putovanja po korisniku (mikroservis TravelPlansApi).
  Pokreni nakon postojećih migracija (Users mora postojati).
*/
IF OBJECT_ID(N'dbo.TravelPlans', N'U') IS NOT NULL
    DROP TABLE dbo.TravelPlans;
GO

CREATE TABLE dbo.TravelPlans
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelPlans PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    ShortDescription NVARCHAR(500) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    PlannedBudget DECIMAL(18, 2) NOT NULL,
    GeneralNotes NVARCHAR(4000) NULL,
    CreatedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL,
    CONSTRAINT FK_TravelPlans_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_TravelPlans_UserId_StartDate
    ON dbo.TravelPlans (UserId, StartDate DESC);
GO
