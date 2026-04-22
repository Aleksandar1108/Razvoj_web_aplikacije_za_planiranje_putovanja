/*
  DEV — aplikacija koristi SQL login [sa] (connection stringovi u SF / appsettings).

  U SSMS: Server Properties → Security → "SQL Server and Windows Authentication mode"
  → restart SQL Server servisa.

  Pokreni kao administrator. Postavlja lozinku za [sa] da odgovara dev stringu.
*/

USE [master];
GO

ALTER LOGIN [sa] ENABLE;
GO

ALTER LOGIN [sa] WITH PASSWORD = N'Str0ng!Pass123', CHECK_POLICY = OFF;
GO
