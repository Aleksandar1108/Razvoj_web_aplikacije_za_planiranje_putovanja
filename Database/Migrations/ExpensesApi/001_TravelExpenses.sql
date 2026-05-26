USE PlaniranjePutovanja_Expenses;
GO

IF OBJECT_ID(N'dbo.TravelExpenses', N'U') IS NOT NULL DROP TABLE dbo.TravelExpenses;
GO

CREATE TABLE dbo.TravelExpenses (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TravelExpenses PRIMARY KEY,
    TravelPlanId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    Amount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_TravelExpenses_Amount DEFAULT (0),
    ExpenseDate DATE NOT NULL,
    Description NVARCHAR(2000) NULL,
    CreatedAtUtc DATETIME2(3) NOT NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL,
    CONSTRAINT CK_TravelExpenses_Category CHECK (Category IN ('transport', 'accommodation', 'food', 'tickets', 'shopping', 'other'))
);

CREATE NONCLUSTERED INDEX IX_TravelExpenses_TravelPlanId_Date
    ON dbo.TravelExpenses (TravelPlanId, ExpenseDate);
GO
