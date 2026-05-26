USE PlaniranjePutovanja_Web1;
GO

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NOT NULL DROP TABLE dbo.RefreshTokens;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

CREATE TABLE dbo.Roles (
    Id          TINYINT         NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Name        NVARCHAR(32)    NOT NULL,
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);

INSERT INTO dbo.Roles (Id, Name) VALUES (1, N'User'), (2, N'Admin');
GO

CREATE TABLE dbo.Users (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
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

CREATE TABLE dbo.RefreshTokens (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId              UNIQUEIDENTIFIER NOT NULL,
    TokenHash           NVARCHAR(128)    NOT NULL,
    ExpiresAtUtc        DATETIME2(3)     NOT NULL,
    CreatedAtUtc        DATETIME2(3)     NOT NULL CONSTRAINT DF_RT_Created DEFAULT (SYSUTCDATETIME()),
    RevokedAtUtc        DATETIME2(3)     NULL,
    ReplacedByTokenId   UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash) WHERE RevokedAtUtc IS NULL;
CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
GO
