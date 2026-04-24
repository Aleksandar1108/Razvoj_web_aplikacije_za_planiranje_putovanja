/*
  Checklist / packing lista po planu putovanja (mikroservis ChecklistApi).
  Pokreni nakon 008_TravelExpenses.sql.
*/
IF DB_NAME() <> N'PlaniranjePutovanja'
    PRINT N'Upozorenje: skripta se izvršava nad bazom [' + DB_NAME() + N'], očekivano je [PlaniranjePutovanja].';
GO

IF OBJECT_ID(N'dbo.TravelPlans', N'U') IS NULL
    THROW 50001, 'Nedostaje tabela dbo.TravelPlans. Pokreni prvo 005_TravelPlans.sql nad istom bazom.', 1;
GO

IF OBJECT_ID(N'dbo.TravelChecklistItems', N'U') IS NOT NULL
    DROP TABLE dbo.TravelChecklistItems;
GO

CREATE TABLE dbo.TravelChecklistItems
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelChecklistItems PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    IsDone BIT NOT NULL CONSTRAINT DF_TravelChecklistItems_IsDone DEFAULT (0),
    CreatedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL,
    CONSTRAINT FK_TravelChecklistItems_TravelPlans FOREIGN KEY (TravelPlanId)
        REFERENCES dbo.TravelPlans (Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_TravelChecklistItems_TravelPlanId_CreatedAt
    ON dbo.TravelChecklistItems (TravelPlanId, CreatedAtUtc);
GO
