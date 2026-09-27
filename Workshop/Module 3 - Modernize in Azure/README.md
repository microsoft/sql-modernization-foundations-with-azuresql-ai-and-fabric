# Module 3 - Modernize ZavaFin with AI in Azure SQL

In Module 2, you migrated the ZavaFin lending database to Azure. In this module, you will first examine the operational modernization benefits provided by Azure SQL Managed Instance, then modernize the workload where its data already lives. You will add native vector capabilities, natural-language hybrid search, and AI-assisted loan scoring to SQL, then use those capabilities from the existing Zava Lending web application.

In this module, you will modernize the app without changing the code. Its screens, controllers, repositories, and stored procedure calls remain the same while you improve what the application can do by changing only the database objects behind its existing contracts.

## What you will learn

By the end of this module, you will be able to:

- explain the modernization benefits of Azure SQL Managed Instance;
- explain why full-text search alone cannot answer meaning-based questions;
- register an embedding model for use from T-SQL;
- generate and store vector embeddings with `AI_GENERATE_EMBEDDINGS`;
- create a DiskANN vector index and maintain it through normal DML;
- combine semantic similarity with relational filters in one hybrid query;
- ground an AI risk assessment with similar historical loans;
- keep the final underwriting decision with a human loan officer; and
- modernize the app without changing the code.

## Module route

Complete the exercises in order. The target times total 80 minutes, leaving about 10 minutes for setup and troubleshooting within a 90-minute session.

| Exercise                                                                            | Focus                                                 | Scripts   | Target |
| ----------------------------------------------------------------------------------- | ----------------------------------------------------- | --------- | ------ |
| [3.0 About ZavaFin](<./00 - About ZavaFin/README.md>)                                 | Scenario and application baseline                     | None      | 5 min  |
| [3.1 SQL Managed Instance Benefits](<./01 - SQL Managed Instance Benefits/README.md>) | Managed-service modernization benefits                 | None      | 5 min  |
| [3.2 AI Foundations](<./02 - AI Foundations/README.md>)                               | External model, embeddings, vector index, and DML     | `01`-`04` | 25 min |
| [3.3 Natural Language Search](<./03 - Natural Language Search/README.md>)             | Semantic search plus relational filters               | `05`-`06` | 15 min |
| [3.4 AI-Assisted Loan Scoring](<./04 - AI-Assisted Loan Scoring/README.md>)           | Human baseline, grounded assessment, and safety audit | `07`-`09` | 25 min |
| [3.5 Summary](<./05 - Summary/README.md>)                                             | Outcomes and Microsoft platform value                 | None      | 5 min  |

> [!IMPORTANT]
> Run every query against only your assigned `ZavaLendingDB-No` database. Replace `No` with your workshop number. Do not use another attendee's database.

## Shared prerequisites

You need:

- the temporary workshop account and database assigned by the instructor;
- a completed [Module 2 migration](<../Module 2 - Migrate to Azure/README.md>);
- SQL Server Management Studio (SSMS) connected to the workshop target;
- the Microsoft Foundry model endpoints and database-scoped credentials prepared for the workshop;
- the [self-contained Zava Lending package](../../dist/ZavaLending-win-x64.zip), or [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) to run the application from source; and
- a terminal opened in this Module 3 folder.

In SSMS, open a new query window, select your assigned database, and verify the connection:

```sql
SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    ORIGINAL_LOGIN() AS LoginName;
```

Confirm that `DatabaseName` is your `ZavaLendingDB-No`.

> [!NOTE]
> [00-reset.sql](./scripts/00-reset.sql) is an instructor/troubleshooting reset, not one of the nine module steps. Do not run it during the normal flow unless an instructor asks you to reset the exercise.

## (optional) Start the legacy Zava Lending application

Module 3 shows how to modernize the app without changing the code by improving the database objects behind its existing contracts.

Running the app is optional - you can experience Module 3 purely from the SQL side (working with SQL scripts and queries), but we recommend that you also run the application.

The app is available as a self-contained Windows package and in the [ZavaLending.Web project](./apps/zava-lending/src/ZavaLending.Web/ZavaLending.Web.csproj). Choose one option, configure it once, leave it running, and revisit the same screens at each exercise checkpoint.

### Option A: run the self-contained Windows package

This option does not require the .NET SDK.

1. Copy [ZavaLending-win-x64.zip](../../dist/ZavaLending-win-x64.zip) to your machine and extract all files to a local folder.
2. Open PowerShell in the extracted folder.
3. Choose the authentication method that matches your workshop connection.
  For Microsoft Entra authentication:
   ```powershell
   Copy-Item `
     .\appsettings.Local.EntraAuth.example.json `
     .\appsettings.Local.json
   ```
   For SQL authentication:
   ```powershell
   Copy-Item `
     .\appsettings.Local.SqlAuth.example.json `
     .\appsettings.Local.json
   ```
4. Open `.\appsettings.Local.json` and replace:
  - `YOUR_SERVER` with the target server name;
  - `YOUR_DATABASE` with your assigned `ZavaLendingDB-No`; and
  - for SQL authentication only, `YOUR_SQL_USERNAME` and `YOUR_SQL_PASSWORD`.
5. Start the app:
  ```powershell
   .\ZavaLending.Web.exe
  ```
6. Open the HTTP address shown in the terminal.

### Option B: run from source

1. Open PowerShell in the Module 3 folder.
2. Choose the authentication method that matches your workshop connection.
  For Microsoft Entra authentication:
   ```powershell
   Copy-Item `
     .\apps\zava-lending\src\ZavaLending.Web\appsettings.Local.EntraAuth.example.json `
     .\apps\zava-lending\src\ZavaLending.Web\appsettings.Local.json
   ```
   For SQL authentication:
   ```powershell
   Copy-Item `
     .\apps\zava-lending\src\ZavaLending.Web\appsettings.Local.SqlAuth.example.json `
     .\apps\zava-lending\src\ZavaLending.Web\appsettings.Local.json
   ```
3. Open the newly created `.\apps\zava-lending\src\ZavaLending.Web\appsettings.Local.json` and replace:
  - `YOUR_SERVER` with the target server name;
  - `YOUR_DATABASE` with your assigned `ZavaLendingDB-No`; and
  - for SQL authentication only, `YOUR_SQL_USERNAME` and `YOUR_SQL_PASSWORD`.
4. Start the app:
  ```powershell
   dotnet run --project .\apps\zava-lending\src\ZavaLending.Web
  ```
5. Open [http://localhost:5126](http://localhost:5126), or the address shown in the terminal.

> [!CAUTION]
> `appsettings.Local.json` can contain a password. Never commit, share, or paste its contents into chat. Use only the temporary workshop credentials.

The application detects the capabilities deployed in the connected database. Its badges change from **Full-Text Search** to **Hybrid Search** and from **Human Review** to **AI-Assisted**, demonstrating how you can modernize the app without changing the code. Capability results are cached briefly; after a script finishes, wait up to 30 seconds and refresh the page. If necessary, stop the app with `Ctrl+C` and start it again.

Start with [Exercise 3.0 - About ZavaFin](<./00 - About ZavaFin/README.md>).

## Troubleshooting

| Symptom                                              | Check                                                                                                                 |
| ---------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| A script changes the wrong database                  | Stop immediately. Verify `DB_NAME()` and reconnect to your assigned `ZavaLendingDB-No`.                               |
| A required database-scoped credential does not exist | Confirm the database, then ask the instructor to provision the workshop credential. Do not add a key to a script.     |
| The app says database access is not configured       | Verify `appsettings.Local.json`, the server, database name, authentication mode, and credentials.                     |
| The app badge does not change after a script         | Wait 30 seconds and refresh, or restart the app to clear its capability cache.                                        |
| Port `5126` is already in use                        | Run `dotnet run --project .\apps\zava-lending\src\ZavaLending.Web -- --urls http://localhost:5200` and open that URL. |
