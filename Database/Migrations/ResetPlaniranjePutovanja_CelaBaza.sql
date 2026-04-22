/*
  RESET: briše celu bazu PlaniranjePutovanja i pravi je ispočetka
  sa istom šemom kao na početku (Roles, Users, RefreshTokens).

  Pokreni u SSMS (ceo skript odjednom). Zahteva prava za DROP/CREATE DATABASE.

  NAPOMENA: ako si tabele slučajno imao u bazi master, ova skripta to NE dira —
  samo bazu PlaniranjePutovanja.
*/

USE master;
SET NOCOUNT ON;
GO

IF DB_ID(N'PlaniranjePutovanja') IS NOT NULL
BEGIN
    ALTER DATABASE PlaniranjePutovanja SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PlaniranjePutovanja;
END
GO

CREATE DATABASE PlaniranjePutovanja;
GO

USE PlaniranjePutovanja;
GO

CREATE TABLE dbo.Roles (
    Id          TINYINT         NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Name        NVARCHAR(32)    NOT NULL,
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);

INSERT INTO dbo.Roles (Id, Name) VALUES (1, N'User'), (2, N'Admin');
GO

CREATE TABLE dbo.Users (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY
        DEFAULT NEWSEQUENTIALID(),
    FirstName       NVARCHAR(100)    NOT NULL,
    LastName        NVARCHAR(100)    NOT NULL,
    Email           NVARCHAR(256)    NOT NULL,
    PasswordHash    NVARCHAR(500)    NOT NULL,
    RoleId          TINYINT          NOT NULL,
    IsActive        BIT              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAtUtc    DATETIME2(3)     NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id)
);

CREATE INDEX IX_Users_Email ON dbo.Users (Email);
GO

CREATE TABLE dbo.TravelPlans (
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

CREATE NONCLUSTERED INDEX IX_TravelPlans_UserId_StartDate
    ON dbo.TravelPlans (UserId, StartDate DESC);
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

CREATE NONCLUSTERED INDEX IX_TravelDestinations_TravelPlanId_Arrival
    ON dbo.TravelDestinations (TravelPlanId, ArrivalDate);
GO

CREATE TABLE dbo.RefreshTokens (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY
        DEFAULT NEWSEQUENTIALID(),
    UserId              UNIQUEIDENTIFIER NOT NULL,
    TokenHash           NVARCHAR(128)    NOT NULL,
    ExpiresAtUtc        DATETIME2(3)     NOT NULL,
    CreatedAtUtc        DATETIME2(3)     NOT NULL CONSTRAINT DF_RT_Created DEFAULT (SYSUTCDATETIME()),
    RevokedAtUtc        DATETIME2(3)     NULL,
    ReplacedByTokenId   UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash)
    WHERE RevokedAtUtc IS NULL;

CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
GO

PRINT N'Gotovo: baza PlaniranjePutovanja je prazna šema spremna za registraciju.';
GO
