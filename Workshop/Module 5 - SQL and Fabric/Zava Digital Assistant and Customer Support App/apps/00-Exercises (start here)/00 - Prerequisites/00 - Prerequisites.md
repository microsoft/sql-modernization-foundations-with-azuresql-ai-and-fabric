# Prerequisites

For this workshop, you will use Windows, a browser, Visual Studio Code, the **.NET 10 SDK**, and **Azure CLI**.

## Section 1: Check your tools

### Task 1.1: Check the installed versions

1. Open **PowerShell** or a PowerShell terminal in VS Code.
2. Run:
  ```powershell
   dotnet --list-sdks
   az version
  ```
3. Check that the SDK list includes `10.0.xxx` and **Azure CLI** displays its version.

### Task 1.2: Install any missing tools

Run only the commands for tools you are missing:

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact
winget install --id Microsoft.AzureCLI --exact
winget install --id Microsoft.VisualStudioCode --exact
```

Reopen VS Code and your terminal after installation, then repeat the version checks.

> [!NOTE]
> Install the .NET **SDK**, not just the runtime. If installation requires approval or is blocked by your organization, ask IT or a workshop helper.

**Check:** Both version commands complete successfully.

If a command is still not recognized after reopening the terminal, ask a helper. You can also use the official [.NET installation](https://dotnet.microsoft.com/download/dotnet/10.0) and [Azure CLI installation](https://learn.microsoft.com/cli/azure/install-azure-cli-windows) instructions.

## Next steps

You can now continue to [Getting Started](../01%20-%20Getting%20Started/01%20-%20Getting%20Started.md).
