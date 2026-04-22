/*
  Destinacije po planu putovanja (mikroservis DestinationsApi).
  Pokreni nakon 005_TravelPlans.sql.
*/
IF OBJECT_ID(N'dbo.TravelDestinations', N'U') IS NOT NULL
    DROP TABLE dbo.TravelDestinations;
GO

CREATE TABLE dbo.TravelDestinations
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelDestinations PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Location NVARCHAR(300) NOT NULL,
    ArrivalDate DATE NOT NULL,
    DepartureDate DATE NOT NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL,
    CONSTRAINT FK_TravelDestinations_TravelPlans FOREIGN KEY (TravelPlanId)
        REFERENCES dbo.TravelPlans (Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_TravelDestinations_TravelPlanId_Arrival
    ON dbo.TravelDestinations (TravelPlanId, ArrivalDate);
GO
