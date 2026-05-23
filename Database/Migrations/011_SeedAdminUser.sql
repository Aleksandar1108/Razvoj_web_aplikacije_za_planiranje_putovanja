/*
  Početni admin nalog (Web1 / BCrypt.Net-Next, work factor 11).
  Lozinka: Admin123!
  Hash generisan BCrypt-om kompatibilnim sa BCrypt.Net-Next (Python bcrypt → $2b$).

  Pokreni nad bazom gde već postoje dbo.Roles i dbo.Users (nakon 001_Auth_RolesAndUsers.sql).
*/

SET NOCOUNT ON;

DECLARE @Email NVARCHAR(256) = N'admin@gmail.com';

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
BEGIN
    INSERT INTO dbo.Users (Id, FirstName, LastName, Email, PasswordHash, RoleId, IsActive, CreatedAtUtc)
    VALUES (
        'A0000001-0001-0001-0001-000000000001',
        N'Sistem',
        N'Administrator',
        @Email,
        N'$2b$11$N7t5WwqyWOcr7HqmkMDK0OurYMpepbmU1PbN9mlpbmMsM28STcCh.',
        2, /* Admin */
        1,
        SYSUTCDATETIME()
    );
END
GO
