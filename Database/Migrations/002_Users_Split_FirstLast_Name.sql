/*
  Migracija: FullName -> FirstName + LastName (ako je stara šema imala samo FullName).

  Problem koji si video: SQL Server kompajlira ceo batch i proverava kolone i u IF granama.
  Zato UPDATE / DROP COLUMN moraju biti u dinamičkom SQL-u (sp_executesql) — tek se tada kompajliraju.

  Ako si već pokrenuo NOVU 001 (FirstName + LastName, bez FullName) — ovu skriptu NE MORAŠ pokretati.

  Pokreni ceo fajl u SSMS.
*/

USE PlaniranjePutovanja;
GO

/* 1) Dodaj kolone ako nedostaju */
IF COL_LENGTH(N'dbo.Users', N'FirstName') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD FirstName NVARCHAR(100) NOT NULL
        CONSTRAINT DF_Users_FirstName_Mig DEFAULT (N'');
    ALTER TABLE dbo.Users ADD LastName NVARCHAR(100) NOT NULL
        CONSTRAINT DF_Users_LastName_Mig DEFAULT (N'');
END
GO

/* 2) Kopiraj podatke iz FullName (samo ako sve tri kolone postoje u trenutku izvršenja) */
IF COL_LENGTH(N'dbo.Users', N'FullName') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'FirstName') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'LastName') IS NOT NULL
BEGIN
    DECLARE @migrate nvarchar(max) = N'
UPDATE dbo.Users
SET
    FirstName = CASE
        WHEN CHARINDEX(CHAR(32), LTRIM(RTRIM(FullName))) > 0
            THEN LEFT(LTRIM(RTRIM(FullName)), CHARINDEX(CHAR(32), LTRIM(RTRIM(FullName))) - 1)
        ELSE LTRIM(RTRIM(FullName))
    END,
    LastName = CASE
        WHEN CHARINDEX(CHAR(32), LTRIM(RTRIM(FullName))) > 0
            THEN LTRIM(SUBSTRING(
                LTRIM(RTRIM(FullName)),
                CHARINDEX(CHAR(32), LTRIM(RTRIM(FullName))) + 1,
                500))
        ELSE ''''
    END;
';
    EXEC sys.sp_executesql @migrate;
END
GO

/* 3) Ukloni FullName (dinamički, da spoljašnji batch ne kompajlira nepostojeću kolonu) */
IF COL_LENGTH(N'dbo.Users', N'FullName') IS NOT NULL
    EXEC (N'ALTER TABLE dbo.Users DROP COLUMN FullName');
GO

/* 4) Ukloni privremene DEFAULT-e */
IF EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
      AND dc.name = N'DF_Users_FirstName_Mig'
)
    ALTER TABLE dbo.Users DROP CONSTRAINT DF_Users_FirstName_Mig;

IF EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
      AND dc.name = N'DF_Users_LastName_Mig'
)
    ALTER TABLE dbo.Users DROP CONSTRAINT DF_Users_LastName_Mig;
GO
