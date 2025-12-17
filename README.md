# BiblioFlow
Digital library (MIAGE/MBDS): BackOffice (Razor Pages + EF Core) and FrontOffice (MVC + ADO.NET + REST API) on SQL Server. Borrowing = time-limited online access (no permanent download). Includes pagination, CSV import, PDF export, and indexing.

# BiblioFlow — Bibliothèque numérique (Modèle A)

BiblioFlow est une bibliothèque numérique où **emprunter = obtenir un droit d'accès temporaire** à la lecture en ligne (PDF streamé).
Aucun téléchargement permanent n'est nécessaire : **à l'échéance, l'API bloque l'accès**.

## Stack (conforme aux exigences)
- **BackOffice (Admin)** : SQL Server + Entity Framework Core + Razor Pages
- **FrontOffice (Lecteurs)** : SQL Server + ADO.NET + MVC + REST API
- Autres : Pagination, Import CSV, Export PDF, Indexation, CSS

## Architecture
- `Biblio.BackOffice` : administration du catalogue (livres, licences, imports/exports)
- `Biblio.FrontOffice` : catalogue public (MVC) + endpoints REST (emprunt/retour/lecture)

---

## Prérequis
- .NET SDK (via `dotnet --info`)
- Docker (pour SQL Server)
- Ports par défaut :
  - SQL Server : `1433`
  - BackOffice : `5100`
  - FrontOffice : `5200`

---

## Démarrage rapide

### 1) SQL Server (Docker)
À la racine :
```bash
docker run -d --name sql1 \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=Root12345678" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
