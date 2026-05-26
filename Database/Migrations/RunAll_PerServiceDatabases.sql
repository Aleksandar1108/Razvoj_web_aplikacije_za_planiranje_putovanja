/*
  Kreira sve baze (1 servis = 1 baza) i primenjuje SQL migracije po servisu.

  SSMS: Query -> SQLCMD Mode (mora biti ukljucen), zatim F5.
  Ako :r ne radi, pokreni Run-Migrations.ps1 iz ovog foldera.

  Ako je repozitorijum na drugom disku, izmeni MigrationsRoot ispod.
*/
:setvar MigrationsRoot "c:\Users\Windows 10\Documents\GitHub\Razvoj_web_aplikacije_za_planiranje_putovanja\Database\Migrations"

SET NOCOUNT ON;
GO

:r $(MigrationsRoot)\Web1\000_CreateDatabase.sql
:r $(MigrationsRoot)\Web1\001_Auth.sql
:r $(MigrationsRoot)\Web1\002_UserNotifications.sql
:r $(MigrationsRoot)\Web1\003_SeedAdminUser.sql
GO

:r $(MigrationsRoot)\TravelPlansApi\000_CreateDatabase.sql
:r $(MigrationsRoot)\TravelPlansApi\001_TravelPlans.sql
GO

:r $(MigrationsRoot)\SharingApi\000_CreateDatabase.sql
:r $(MigrationsRoot)\SharingApi\001_TravelPlanSharing.sql
GO

:r $(MigrationsRoot)\DestinationsApi\000_CreateDatabase.sql
:r $(MigrationsRoot)\DestinationsApi\001_TravelDestinations.sql
GO

:r $(MigrationsRoot)\ActivitiesApi\000_CreateDatabase.sql
:r $(MigrationsRoot)\ActivitiesApi\001_TravelActivities.sql
GO

:r $(MigrationsRoot)\ExpensesApi\000_CreateDatabase.sql
:r $(MigrationsRoot)\ExpensesApi\001_TravelExpenses.sql
GO

:r $(MigrationsRoot)\ChecklistApi\000_CreateDatabase.sql
:r $(MigrationsRoot)\ChecklistApi\001_TravelChecklistItems.sql
GO

PRINT N'Sve per-servis baze su kreirane i migrirane.';
