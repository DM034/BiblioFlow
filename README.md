# BiblioFlow
Digital library (MIAGE/MBDS): BackOffice (Razor Pages + EF Core) and FrontOffice (MVC + ADO.NET + REST API) on SQL Server. Borrowing = time-limited online access (no permanent download). Includes pagination, CSV import, PDF export, and indexing.

````md
# BiblioFlow (FrontOffice + BackOffice) — Full Setup (Windows & Linux)

A .NET solution (FrontOffice MVC + BackOffice Razor Pages) using SQL Server (Docker recommended).

---

## 0) Project Structure

- `Biblio.FrontOffice` : FrontOffice (ASP.NET Core MVC) — catalog, search, borrow, read PDF
- `Biblio.BackOffice`  : BackOffice (Razor Pages) — admin (Books, Licenses, Loans, Import/Export, Upload PDF…)
- `docker/`            : optional compose / scripts

---

## 1) Requirements

### Mandatory
- **.NET SDK** (target framework: `net10.0`)
  - Check:
    ```bash
    dotnet --version
    ```
- **SQL Server** (Docker recommended)
- **Docker** (recommended)
  - Windows: Docker Desktop
  - Linux: Docker Engine + Docker Compose plugin
  - Check:
    ```bash
    docker --version
    docker compose version
    ```

### Recommended
- Git
- VS Code / Visual Studio

---

## 2) NuGet Packages (What you typically need)

### Data Access / SQL
- `Microsoft.Data.SqlClient`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools`

### BackOffice (PDF export, if used)
- `QuestPDF`

### Optional CLI tools
- EF CLI:
  ```bash
  dotnet tool install -g dotnet-ef
````

* Razor scaffolding:

  ```bash
  dotnet tool install -g dotnet-aspnet-codegenerator
  ```

> To list installed packages in a project:

```bash
dotnet list package
```

---

## 3) Database (SQL Server) — Docker Setup (Recommended)

### 3.1 Start SQL Server

#### Option A — docker run

```bash
docker run -d --name sql1biblio \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=Root12345678" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

Verify container is running:

```bash
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

#### Option B — docker compose

If you have a `docker-compose.yml`:

```bash
docker compose up -d
```

### 3.2 Test SQL Server connectivity (inside container)

```bash
docker exec -it sql1biblio /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P "Root12345678" \
  -Q "SELECT @@VERSION;"
```

---

## 4) Connection Strings (FrontOffice + BackOffice)

### 4.1 BackOffice (`Biblio.BackOffice/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=BiblioDb;User Id=sa;Password=Root12345678;TrustServerCertificate=True;Encrypt=False"
  }
}
```

### 4.2 FrontOffice (`Biblio.FrontOffice/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=BiblioDb;User Id=sa;Password=Root12345678;TrustServerCertificate=True;Encrypt=False"
  }
}
```

Notes:

* If you hit TLS/handshake errors, keep:

  * `Encrypt=False`
  * `TrustServerCertificate=True`
* If SQL runs on your host and apps run on the host: `localhost,1433` works (because you exposed `-p 1433:1433`).

---

## 5) EF Core Migrations / Create the Database

Run from the project that contains the `DbContext` and migrations (usually BackOffice).

```bash
cd Biblio.BackOffice
dotnet restore
dotnet ef database update
```

---

## 6) Run the Apps

### 6.1 Run BackOffice

```bash
cd Biblio.BackOffice
dotnet restore
dotnet run --urls "http://0.0.0.0:5100"
```

Useful routes:

* `http://localhost:5100/Login`
* `http://localhost:5100/Admin/Books`
* `http://localhost:5100/Admin/Licenses`
* `http://localhost:5100/Admin/Loans`
* `http://localhost:5100/Admin/Import`
* `http://localhost:5100/Admin/Export`

### 6.2 Run FrontOffice

```bash
cd Biblio.FrontOffice
dotnet restore
dotnet run --urls "http://0.0.0.0:5200"
```

Useful routes:

* `http://localhost:5200/Books`
* `http://localhost:5200/Account/Login`

---

## 7) Seed Test Data (Insert a Book + License)

Example: insert one book and one license, then list latest rows.

```bash
docker exec -i sql1biblio /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P "Root12345678" \
  -Q "USE BiblioDb;
INSERT INTO Books(Title, Author, Category, Year, Summary, PdfPath)
VALUES (N'Livre1', N'Auteur1', N'General', 2025, N'Resume', N'/home/dm/Documents/DOSSIER M2/DM/PDF/02_IDENTITE.pdf');
DECLARE @id INT = SCOPE_IDENTITY();
INSERT INTO Licenses(BookId, ConcurrentSeats) VALUES (@id, 1);
SELECT TOP 5 Id, Title, PdfPath FROM Books ORDER BY Id DESC;"
```

---

## 8) How `PdfPath` Works (Important)

`PdfPath` is a **server-side file path**. When FrontOffice calls `Read`, it typically does:

* `File.Exists(path)`
* then streams the PDF from disk.

So the PDF file must exist **on the machine where FrontOffice is running**.

### Case A — FrontOffice runs on the host machine (your Linux/Windows)

✅ Store an absolute host path, e.g.

* Linux: `/home/dm/.../file.pdf`
* Windows: `C:\Users\...\file.pdf`

### Case B — FrontOffice runs in Docker

✅ You must mount a volume and use an in-container path, e.g. `/pdfs/file.pdf`.

---

## 9) Clean / Rebuild Commands

### Linux / macOS

```bash
rm -rf bin obj
dotnet build
```

### Windows (PowerShell)

```powershell
Remove-Item -Recurse -Force .\bin, .\obj
dotnet build
```

---

## 10) Common Issues

### A) SQL Server not found / connection fails

* SQL container not running:

  ```bash
  docker ps
  ```
* Port not exposed:

  * ensure `-p 1433:1433`
* Wrong connection string.

### B) Pre-login handshake / TLS reset errors

Use:

* `Encrypt=False;TrustServerCertificate=True`

### C) “The ConnectionString property has not been initialized”

* Ensure `appsettings.json` has:

  * `"ConnectionStrings": { "Default": "..." }`
* Ensure code uses:

  ```csharp
  cfg.GetConnectionString("Default")
  ```

---

## 11) BackOffice Login

BackOffice auth is based on your implementation (session like `admin_email`).

* Login route:

  * `/Login`
* After login, session should contain `admin_email` to unlock admin pages.

---

## 12) Quick Start (All-in-one)

```bash
# 1) Start SQL Server
docker run -d --name sql1biblio -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Root12345678" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# 2) Apply migrations (BackOffice)
cd Biblio.BackOffice
dotnet restore
dotnet ef database update

# 3) Run BackOffice
dotnet run --urls "http://0.0.0.0:5100"

# 4) Run FrontOffice
cd ../Biblio.FrontOffice
dotnet restore
dotnet run --urls "http://0.0.0.0:5200"
```

---

```
::contentReference[oaicite:0]{index=0}
```
