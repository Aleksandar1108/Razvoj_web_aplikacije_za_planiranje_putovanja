/*
  Troškovi po planu putovanja (mikroservis ExpensesApi).
  Pokreni nakon 007_TravelActivities.sql.
*/
IF DB_NAME() <> N'PlaniranjePutovanja'
    PRINT N'Upozorenje: skripta se izvršava nad bazom [' + DB_NAME() + N'], očekivano je [PlaniranjePutovanja].';
GO

IF OBJECT_ID(N'dbo.TravelPlans', N'U') IS NULL
    THROW 50001, 'Nedostaje tabela dbo.TravelPlans. Pokreni prvo 005_TravelPlans.sql nad istom bazom.', 1;
GO

IF OBJECT_ID(N'dbo.TravelExpenses', N'U') IS NOT NULL
    DROP TABLE dbo.TravelExpenses;
GO

CREATE TABLE dbo.TravelExpenses
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelExpenses PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_TravelExpenses_Amount DEFAULT (0),
    ExpenseDate DATE NOT NULL,
    Description NVARCHAR(2000) NULL,
    CreatedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL,
    CONSTRAINT FK_TravelExpenses_TravelPlans FOREIGN KEY (TravelPlanId)
        REFERENCES dbo.TravelPlans (Id) ON DELETE CASCADE,
    CONSTRAINT CK_TravelExpenses_Category CHECK (Category IN ('transport', 'accommodation', 'food', 'tickets', 'shopping', 'other'))
);
GO

CREATE NONCLUSTERED INDEX IX_TravelExpenses_TravelPlanId_Date
    ON dbo.TravelExpenses (TravelPlanId, ExpenseDate);
GO
