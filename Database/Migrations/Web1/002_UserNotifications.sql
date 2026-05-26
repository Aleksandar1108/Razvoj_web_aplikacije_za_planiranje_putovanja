USE PlaniranjePutovanja_Web1;
GO

IF OBJECT_ID(N'dbo.UserNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserNotifications (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserNotifications PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        TravelPlanId UNIQUEIDENTIFIER NULL,
        TravelDestinationId UNIQUEIDENTIFIER NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_UserNotifications_IsRead DEFAULT (0),
        CreatedAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT FK_UserNotifications_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_UserNotifications_UserId_IsRead_Created
        ON dbo.UserNotifications (UserId, IsRead, CreatedAtUtc DESC);
END
GO
