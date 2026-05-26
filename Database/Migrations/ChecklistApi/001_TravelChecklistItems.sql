USE PlaniranjePutovanja_Checklist;
GO

IF OBJECT_ID(N'dbo.TravelChecklistItems', N'U') IS NOT NULL DROP TABLE dbo.TravelChecklistItems;
GO

CREATE TABLE dbo.TravelChecklistItems (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelChecklistItems PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    IsDone BIT NOT NULL CONSTRAINT DF_TravelChecklistItems_IsDone DEFAULT (0),
    CreatedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL
);

CREATE NONCLUSTERED INDEX IX_TravelChecklistItems_TravelPlanId_CreatedAt
    ON dbo.TravelChecklistItems (TravelPlanId, CreatedAtUtc);
GO
