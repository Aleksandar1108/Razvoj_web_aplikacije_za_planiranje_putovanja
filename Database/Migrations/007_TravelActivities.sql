/*
  Aktivnosti po danima (mikroservis ActivitiesApi).
  Pokreni nakon 006_TravelDestinations.sql.
*/
IF DB_NAME() <> N'PlaniranjePutovanja'
    PRINT N'Upozorenje: skripta se izvršava nad bazom [' + DB_NAME() + N'], očekivano je [PlaniranjePutovanja].';
GO

IF OBJECT_ID(N'dbo.TravelPlans', N'U') IS NULL
    THROW 50001, 'Nedostaje tabela dbo.TravelPlans. Pokreni prvo 005_TravelPlans.sql (i po potrebi 006_TravelDestinations.sql) nad istom bazom.', 1;
GO

IF OBJECT_ID(N'dbo.TravelActivities', N'U') IS NOT NULL
    DROP TABLE dbo.TravelActivities;
GO

CREATE TABLE dbo.TravelActivities
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelActivities PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    ActivityDate DATE NOT NULL,
    ActivityTime CHAR(5) NOT NULL,
    Location NVARCHAR(300) NOT NULL,
    Description NVARCHAR(2000) NULL,
    EstimatedCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_TravelActivities_EstimatedCost DEFAULT (0),
    Status NVARCHAR(20) NOT NULL,
    CreatedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL,
    CONSTRAINT FK_TravelActivities_TravelPlans FOREIGN KEY (TravelPlanId)
        REFERENCES dbo.TravelPlans (Id) ON DELETE CASCADE,
    CONSTRAINT CK_TravelActivities_Status CHECK (Status IN ('planned', 'reserved', 'completed', 'cancelled'))
);
GO

CREATE NONCLUSTERED INDEX IX_TravelActivities_TravelPlanId_Date_Time
    ON dbo.TravelActivities (TravelPlanId, ActivityDate, ActivityTime);
GO
