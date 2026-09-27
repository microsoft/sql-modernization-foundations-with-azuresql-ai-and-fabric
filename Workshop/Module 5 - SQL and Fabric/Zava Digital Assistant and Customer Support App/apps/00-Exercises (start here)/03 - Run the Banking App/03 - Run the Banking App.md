# Run the Digital Assistant App (ZavaBanking)

In this section, you will connect the banking app to your Fabric SQL database and the supplied Azure OpenAI deployment.

## Section 1: Configure the connection

### Task 1.1: Sign in through Azure CLI

1. Make sure you are logged in to Azure.
  ```powershell
  az login
  ```
2. Sign in with the **same account you used in Fabric**. Check the account and tenant shown in the terminal.

###

## Section 2: Set up and start the app

**Note**: If yu have not not created initial database schema and populate initial data"), go back to the [previous step](<../02 - Create Your Fabric Database/02 - Create Your Fabric Database.md>).

### Task 2.1: Fill in the app settings

This task is common to both ways of running the app - from deployable package or from VS Code project.

1. Copy the `.env.example` and rename copied version to `.env`

Open `.env` in and replace these values:

<table class="markdown-table">
  <tr><th>Setting</th><th>Value</th></tr>
  <tr><td>ConnectionStrings__ZavaBanking</td><td>Replace &lt;fabric-sql-hostname&gt; and &lt;database-name&gt; with the values from your Fabric database. Leave the rest of the connection string unchanged.</td></tr>
  <tr><td>AzureOpenAI__Endpoint</td><td>Check out <a href="../../../../../Module%201%20-%20Introduction%20and%20Setup/Secrets%20and%20other%20configuration%20parameters.md">Secrets and other configuration parameters</a></td></tr>
  <tr><td>AzureOpenAI__ApiKey</td><td>Check out <a href="../../../../../Module%201%20-%20Introduction%20and%20Setup/Secrets%20and%20other%20configuration%20parameters.md">Secrets and other configuration parameters</a></td></tr>
  <tr><td>AzureOpenAI__Deployment</td><td>Check out <a href="../../../../../Module%201%20-%20Introduction%20and%20Setup/Secrets%20and%20other%20configuration%20parameters.md">Secrets and other configuration parameters</a></td></tr>
</table>

2. Keep the quotes around the values and save your changes.

> [!NOTE]
> For the SQL hostname, omit `tcp:` and `,1433`; the template already includes them.

### Task 2.2: Run Digital Assistant App (ZavaBanking)

Instructions for running the app are provided in the [README](../../../README.md).

### Task 2.3: Create the tables and FAQ data (optional)

**Note**: Execute this task if you have not created initial database schema in [previous step](<../02 - Create Your Fabric Database/02 - Create Your Fabric Database.md>)

1. From the repository root, run:
  ```powershell
  dotnet run --project .\apps\banking\src\ZavaBanking.Web -- --initialize
  ```

Alternatively, you can run this step from a deployable package:

```powershell
.\ZavaBanking.Web.exe  --initialize
```

2. This command builds the app, creates missing tables and FAQ data, and then exits. It does not delete existing conversations or call Azure OpenAI; you will test chat in the final exercise. If initialization fails, resolve the error before continuing.
3. In Fabric, refresh the database **Explorer** and expand **dbo > Tables**. You should see `FaqEntries`, `ChatSessions`, `ChatMessages`, and `ChatEscalations`.
4. Open **New SQL query**, paste the following, and select **Run**:

```sql
SELECT Question, Answer FROM dbo.FaqEntries;

```

These are the fictional banking FAQs used by the chat assistant.

## Next steps

You can now continue to [Enable CES](../04%20-%20Enable%20CES/04%20-%20Enable%20CES.md).
