# SQL migracije — 1 servis = 1 baza

Svaki mikroservis ima sopstvenu SQL Server bazu i folder migracija:

| Servis | Baza | Folder |
|--------|------|--------|
| Web1 | `PlaniranjePutovanja_Web1` | `Web1/` |
| TravelPlansApi | `PlaniranjePutovanja_TravelPlans` | `TravelPlansApi/` |
| SharingApi | `PlaniranjePutovanja_Sharing` | `SharingApi/` |
| DestinationsApi | `PlaniranjePutovanja_Destinations` | `DestinationsApi/` |
| ActivitiesApi | `PlaniranjePutovanja_Activities` | `ActivitiesApi/` |
| ExpensesApi | `PlaniranjePutovanja_Expenses` | `ExpensesApi/` |
| ChecklistApi | `PlaniranjePutovanja_Checklist` | `ChecklistApi/` |

## Pokretanje

### A) PowerShell (najpouzdanije)

```powershell
cd "c:\Users\Windows 10\Documents\GitHub\Razvoj_web_aplikacije_za_planiranje_putovanja\Database\Migrations"
.\Run-Migrations.ps1
```

### B) SSMS

1. Otvori `RunAll_PerServiceDatabases.sql`.
2. **Query → SQLCMD Mode** (mora biti uključen).
3. Proveri da je `MigrationsRoot` na vrhu skripte ispravna putanja do ovog foldera.
4. F5 (Execute).

Ako vidiš *Cannot find directory in the path specified for ":r"* — putanja u `MigrationsRoot` nije dobra ili SQLCMD mode nije uključen.

### C) Ručno

Pokreni `000_CreateDatabase.sql` pa `001_*.sql` u svakom podfolderu, redom iz tabele ispod.

## Stare migracije

Fajlovi `000`–`012` u korenu `Database/Migrations/` odnose se na **staru zajedničku** bazu `PlaniranjePutovanja`. Zadržani su radi reference; za novu arhitekturu koristi podfoldere iz tabele iznad.
