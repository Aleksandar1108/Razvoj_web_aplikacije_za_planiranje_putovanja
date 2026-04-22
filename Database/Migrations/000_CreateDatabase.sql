/*
  Kreiranje baze PlaniranjePutovanja (SQL Server).
  Pokreni u SSMS (možeš ostati na bilo kojoj bazi za prvi IF; USE prebacuje na novu).

  Posle ovoga pokreni 001_Auth_RolesAndUsers.sql dok je u toolbaru izabrana baza PlaniranjePutovanja.
*/

IF DB_ID(N'PlaniranjePutovanja') IS NULL
BEGIN
    CREATE DATABASE PlaniranjePutovanja;
END
ELSE
BEGIN
    PRINT N'Baza PlaniranjePutovanja već postoji.';
END
GO

USE PlaniranjePutovanja;
GO
