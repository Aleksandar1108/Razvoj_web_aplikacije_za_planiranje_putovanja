USE PlaniranjePutovanja_Activities;
GO

IF OBJECT_ID(N'dbo.TravelActivities', N'U') IS NOT NULL DROP TABLE dbo.TravelActivities;
GO

CREATE TABLE dbo.TravelActivities (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelActivities PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    ActivityDate DATE NOT NULL,
    ActivityTime CHAR(5) NOT NULL,
    Location NVARCHAR(300) NOT NULL,
    Description NVARCHAR(2000) NULL,
    EstimatedCost DECIMAL(18, 2) NOT NULL CONSTRAINT DF_TravelActivities_EstimatedCost DEFAULT (0),
    Status NVARCHAR(20) NOT NULL,
    CreatedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL,
    CONSTRAINT CK_TravelActivities_Status CHECK (Status IN ('planned', 'reserved', 'completed', 'cancelled'))
);

CREATE NONCLUSTERED INDEX IX_TravelActivities_TravelPlanId_Date_Time
    ON dbo.TravelActivities (TravelPlanId, ActivityDate, ActivityTime);
GO
