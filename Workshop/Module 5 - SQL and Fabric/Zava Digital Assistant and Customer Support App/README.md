# Building new apps using Microsoft Fabric

This repository contains two independently runnable .NET applications used by the Zava SQLCon workshop:

- **Digital Assistant App (ZavaBanking)** - records customer support escalations in Fabric SQL (/banking subfolder)
- **Customer Support App (ZavaSupport)** - receives escalation events from Azure Event Hubs (/CustomerSupportApp subfolder)

Create a SQL database in Microsoft Fabric, connect the Zava banking app, and stream a support request to Event Hubs using Change Event Streaming (CES).

**Start with [the step-by-step exercises](<apps/00-Exercises (start here)/README.md>).** They cover the Windows prerequisites, Fabric setup, app configuration, and CES.

<img src="apps/00-Exercises%20(start%20here)/01%20-%20Getting%20Started/banking-app-high-level.png" width="800" />

## Repository layout

```text
apps/
  banking/
  00-Exercises (start here)/
  CustomerSupportApp/
ZavaWorkshop.slnx
```

Both applications require .NET SDK 10.0.100. Copy each application's `.env.example` to `.env` in the same application folder and provide the required local configuration. The `.env` files are ignored by Git.

## Run the applications from deployable packages

Each application can be run from its self-contained deployable package (outside of VS Code). The packages are in the **/dist** subfolder: `ZavaBanking-win-x64.zip` for the Digital Assistant App (ZavaBanking) and `ZavaSupport-win-x64.zip` for the Customer Support App (ZavaSupport).

For each application:

1. Copy the application's zip file from **/dist** to your machine.
2. Extract all files to a local folder.
3. Right-click the folder with the extracted files and select **Open in Terminal**.
4. Create `.env` as a copy of `.env.example`:

  ```powershell
  Copy-Item .env.example .env
  ```

5. Open `.env` in an editor and fill in the required configuration values, then save:

  ```powershell
  notepad .env
  ```

  How to set up the `.env` file is explained in detail in the instructions for each application - for the Digital Assistant App (ZavaBanking) see [Run the Banking App](<apps/00-Exercises (start here)/03 - Run the Banking App/03 - Run the Banking App.md>), and for the Customer Support App (ZavaSupport) see [Trace Your Escalation](<apps/00-Exercises (start here)/05 - Trace Your Escalation/05 - Trace Your Escalation.md>). The procedure for setting up the `.env` file is the same regardless of how the app is run - from a deployable package or from the VS Code project.

6. Start the application:

  ```powershell
  # Digital Assistant App (ZavaBanking)
  .\ZavaBanking.Web.exe

  # Customer Support App (ZavaSupport)
  .\ZavaSupport.Web.exe
  ```

7. Open the URL printed in the terminal. Leave the terminal running, and press **Ctrl+C** to stop the application.

## Run the applications from VS Code

This section is optional. You can complete exercises by running applications from deployable packages.

### Build and test

From the repository root:

```powershell
dotnet build .\ZavaWorkshop.slnx
dotnet test .\apps\CustomerSupportApp\tests\ZavaSupport.Web.Tests
```

Run each application in a separate terminal:

```powershell
dotnet run --project .\apps\banking\src\ZavaBanking.Web
dotnet run --project .\apps\CustomerSupportApp\src\ZavaSupport.Web
```

## Next steps

1. [Go to Zava Digital Assistant and Customer App Exercises](<apps/00-Exercises (start here)/README.md>)
