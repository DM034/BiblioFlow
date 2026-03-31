# BiblioFlow

Bibliotheque numerique en .NET 10 avec deux applications web:

- BackOffice en Razor Pages + Entity Framework Core
- FrontOffice en ASP.NET Core MVC + ADO.NET

Le projet utilise SQL Server (Docker recommande) et fournit un script local pour demarrer l'ensemble.

## Etat exact du depot

Structure actuelle (hors .git, bin, obj et librairies vendor sous wwwroot/lib):

```text
.
|-- .gitignore
|-- BiblioFlow.slnx
|-- README.md
|-- Biblio.BackOffice
|   |-- Biblio.BackOffice.csproj
|   |-- Program.cs
|   |-- appsettings.json
|   |-- appsettings.Development.json
|   |-- appsettings.Local.json
|   |-- Data
|   |   |-- Entities.cs
|   |   `-- LibraryDbContext.cs
|   |-- Migrations
|   |   |-- 20251217090125_Init.cs
|   |   |-- 20251217090125_Init.Designer.cs
|   |   |-- 20251219074355_MakePdfPathNullable.cs
|   |   |-- 20251219074355_MakePdfPathNullable.Designer.cs
|   |   |-- 20251219074837_MakeSummaryPdfPathOptional.cs
|   |   |-- 20251219074837_MakeSummaryPdfPathOptional.Designer.cs
|   |   |-- 20260305093806_AddAdminAuditTrailAndImportIndexes.cs
|   |   |-- 20260305093806_AddAdminAuditTrailAndImportIndexes.Designer.cs
|   |   `-- LibraryDbContextModelSnapshot.cs
|   |-- Models
|   |   |-- Book.cs
|   |   |-- License.cs
|   |   `-- Loan.cs
|   |-- Pages
|   |   |-- _ViewImports.cshtml
|   |   |-- _ViewStart.cshtml
|   |   |-- Login.cshtml
|   |   |-- Login.cshtml.cs
|   |   |-- Logout.cshtml
|   |   |-- Logout.cshtml.cs
|   |   |-- Admin
|   |   |   |-- Audit
|   |   |   |   |-- Index.cshtml
|   |   |   |   `-- Index.cshtml.cs
|   |   |   |-- Books
|   |   |   |   |-- Create.cshtml
|   |   |   |   |-- Create.cshtml.cs
|   |   |   |   |-- Delete.cshtml
|   |   |   |   |-- Delete.cshtml.cs
|   |   |   |   |-- Details.cshtml
|   |   |   |   |-- Details.cshtml.cs
|   |   |   |   |-- Edit.cshtml
|   |   |   |   |-- Edit.cshtml.cs
|   |   |   |   |-- ExportPdf.cshtml
|   |   |   |   |-- ExportPdf.cshtml.cs
|   |   |   |   |-- ImportCsv.cshtml
|   |   |   |   |-- ImportCsv.cshtml.cs
|   |   |   |   |-- Index.cshtml
|   |   |   |   |-- Index.cshtml.cs
|   |   |   |   |-- UploadPdf.cshtml
|   |   |   |   `-- UploadPdf.cshtml.cs
|   |   |   |-- Dashboard
|   |   |   |   |-- Index.cshtml
|   |   |   |   `-- Index.cshtml.cs
|   |   |   |-- Export
|   |   |   |   |-- Index.cshtml
|   |   |   |   `-- Index.cshtml.cs
|   |   |   |-- Import
|   |   |   |   |-- Index.cshtml
|   |   |   |   `-- Index.cshtml.cs
|   |   |   |-- Licenses
|   |   |   |   |-- Index.cshtml
|   |   |   |   `-- Index.cshtml.cs
|   |   |   `-- Loans
|   |   |       |-- Index.cshtml
|   |   |       `-- Index.cshtml.cs
|   |   `-- Shared
|   |       |-- _Layout.cshtml
|   |       |-- _Layout.cshtml.css
|   |       `-- _ValidationScriptsPartial.cshtml
|   |-- Properties
|   |   `-- launchSettings.json
|   |-- Services
|   |   `-- AdminAuditService.cs
|   `-- wwwroot
|       |-- favicon.ico
|       |-- css
|       |   |-- admin.css
|       |   `-- site.css
|       `-- js
|           `-- site.js
|-- Biblio.FrontOffice
|   |-- Biblio.FrontOffice.csproj
|   |-- Program.cs
|   |-- appsettings.json
|   |-- appsettings.Development.json
|   |-- should_fail.pdf
|   |-- test.pdf
|   |-- Controllers
|   |   |-- AccountController.cs
|   |   |-- BooksController.cs
|   |   |-- HomeController.cs
|   |   |-- LoansController.cs
|   |   `-- Api
|   |       `-- LoansApiController.cs
|   |-- Data
|   |   `-- SqlLibraryRepository.cs
|   |-- Models
|   |   `-- ErrorViewModel.cs
|   |-- Properties
|   |   `-- launchSettings.json
|   |-- Views
|   |   |-- _ViewImports.cshtml
|   |   |-- _ViewStart.cshtml
|   |   |-- Account
|   |   |   `-- Login.cshtml
|   |   |-- Books
|   |   |   |-- Details.cshtml
|   |   |   |-- Index.cshtml
|   |   |   `-- MyLoans.cshtml
|   |   |-- Home
|   |   |   |-- Index.cshtml
|   |   |   `-- Privacy.cshtml
|   |   |-- Loans
|   |   |   `-- Index.cshtml
|   |   `-- Shared
|   |       |-- Error.cshtml
|   |       |-- _Layout.cshtml
|   |       |-- _Layout.cshtml.css
|   |       `-- _ValidationScriptsPartial.cshtml
|   `-- wwwroot
|       |-- favicon.ico
|       |-- css
|       |   |-- front.css
|       |   `-- site.css
|       `-- js
|           `-- site.js
|-- docker
|   |-- docker-compose.yml
|   `-- should_fail.pdf
`-- scripts
    `-- dev.sh
```

## Stack technique

- .NET SDK: net10.0
- Base de donnees: SQL Server 2022
- BackOffice: ASP.NET Core Razor Pages + EF Core + QuestPDF + CsvHelper
- FrontOffice: ASP.NET Core MVC + Microsoft.Data.SqlClient (ADO.NET)

Packages declares actuellement:

- BackOffice
  - CsvHelper 33.1.0
  - Microsoft.EntityFrameworkCore.Design 10.0.1
  - Microsoft.EntityFrameworkCore.SqlServer 10.0.1
  - Microsoft.EntityFrameworkCore.Tools 10.0.1
  - Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.0
  - QuestPDF 2025.12.0
- FrontOffice
  - Microsoft.Data.SqlClient 6.1.3

## Configuration actuelle

Connection string par defaut dans les deux applications:

```text
Server=127.0.0.1,1433;Database=BiblioDb;User Id=sa;Password=Root12345678;Encrypt=False;TrustServerCertificate=True;
```

BackOffice charge aussi appsettings.Local.json (optionnel), qui contient aujourd'hui:

- Admin.Emails: tracemadaprojet@gmail.com, admin@local

Important:

- La page Login du BackOffice accepte un email non vide et le stocke en session sous admin_email.
- Le middleware bloque les routes /Admin/* si admin_email est absent.

## Demarrage rapide

Prerequis:

- dotnet
- docker
- curl
- lsof

Commande recommandee (macOS/Linux):

```bash
./scripts/dev.sh up
```

Ce que fait le script:

1. Verifie Docker (et tente colima start si Docker n'est pas disponible).
2. Lance (ou cree) le conteneur SQL Server sql1biblio.
3. Restore les deux projets.
4. Applique les migrations EF Core (BackOffice).
5. Lance BackOffice sur le port 5161.
6. Lance FrontOffice sur le port 5193.

URLs par defaut avec le script:

- BackOffice: http://localhost:5161/Login
- FrontOffice: http://localhost:5193/Books

Commandes utiles:

```bash
./scripts/dev.sh status
./scripts/dev.sh logs
./scripts/dev.sh down
./scripts/dev.sh down-all
```

## Demarrage manuel

1. Lancer SQL Server:

```bash
docker compose -f docker/docker-compose.yml up -d
```

2. Appliquer les migrations:

```bash
cd Biblio.BackOffice
dotnet restore
dotnet ef database update
```

3. Lancer BackOffice:

```bash
dotnet run --urls "http://localhost:5161"
```

4. Lancer FrontOffice dans un autre terminal:

```bash
cd ../Biblio.FrontOffice
dotnet restore
dotnet run --urls "http://localhost:5193"
```

## Notes utiles

- Le projet contient des fichiers PDF de test:
  - Biblio.FrontOffice/test.pdf
  - Biblio.FrontOffice/should_fail.pdf
  - docker/should_fail.pdf
- Les dossiers generes bin, obj et .run sont ignores par Git (voir .gitignore).
