/*
  SQL 18456: Login failed for user 'WORKGROUP\TVojRacunar$'
  Service Fabric / Web1 često se izvršava pod mašinskim Windows nalogom (COMPUTERNAME$).
  Pokreni u SSMS kao administrator (login sa pravom da pravi logine).

  1) U poruci greške kopiraj TAČAN naziv korisnika (npr. WORKGROUP\HP-LAPTOP-15SS$).
  2) Zameni dole [WORKGROUP\HP-LAPTOP-15SS$] tim stringom ako je drugačiji.
*/

USE [master];
GO

/* Windows login za mašinski nalog (iz greške 18456) */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'WORKGROUP\HP-LAPTOP-15SS$')
BEGIN
    CREATE LOGIN [WORKGROUP\HP-LAPTOP-15SS$] FROM WINDOWS;
END
GO

USE [PlaniranjePutovanja];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'WORKGROUP\HP-LAPTOP-15SS$')
BEGIN
    CREATE USER [WORKGROUP\HP-LAPTOP-15SS$] FOR LOGIN [WORKGROUP\HP-LAPTOP-15SS$];
END
GO

/* Za dev dovoljno; za produkciju suzi uloge */
ALTER ROLE db_owner ADD MEMBER [WORKGROUP\HP-LAPTOP-15SS$];
GO

/*
  Ako u novoj grešci piše DRUGI Windows nalog (npr. NT AUTHORITY\NETWORK SERVICE),
  u SSMS uradi isto: Security → Logins → New Login → Windows → unesi tačan naziv,
  User Mapping → PlaniranjePutovanja → db_owner (za vežbu).
*/
