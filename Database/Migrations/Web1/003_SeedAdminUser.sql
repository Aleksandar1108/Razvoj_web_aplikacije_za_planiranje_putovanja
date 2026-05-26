USE PlaniranjePutovanja_Web1;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@gmail.com')
BEGIN
    INSERT INTO dbo.Users (Id, FirstName, LastName, Email, PasswordHash, RoleId, IsActive, CreatedAtUtc)
    VALUES (
        'A0000001-0001-0001-0001-000000000001',
        N'Sistem',
        N'Administrator',
        N'admin@gmail.com',
        N'$2b$11$N7t5WwqyWOcr7HqmkMDK0OurYMpepbmU1PbN9mlpbmMsM28STcCh.',
        2,
        1,
        SYSUTCDATETIME()
    );
END
GO
