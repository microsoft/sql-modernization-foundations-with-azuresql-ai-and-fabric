![image](./images/pasted_20260924-003343.png)

# Modernizing Your SQL Estate: From SQL Server to Azure SQL and Fabric for AI-Ready Applications

This guide provides instructions to help you prepare for the workshop.

Before the workshop, please complete the following prerequisites: bring a Windows laptop that allows software installation and access to external websites; install SSMS 22.10 or later with the Hybrid and Migration workload and the Microsoft Authenticator app on your phone; verify access to the Azure portal; and test the provided SQL Server and Azure SQL Managed Instance connections.

To participate in the advanced exercises, you will also need to configure GitHub Copilot, Visual Studio Code, the MSSQL extension, Azure CLI, .NET SDK 10, and the required MCP servers.

Detailed instructions for each step are provided below.

# Technical knowledge

## SQL Server

- Familiarity with core SQL Server concepts, including databases, tables, and schemas
- Familiarity with SQL Server architecture and features such as backup and restore, log shipping, high availability, and Always On availability groups
- Familiarity with SQL Server Management Studio (SSMS)
- Experience writing and running T-SQL queries

## Azure

- Basic understanding of Azure concepts, including subscriptions, resource groups, resources, and regions

## Recommended

- Prior exposure to at least one of the following: Visual Studio Code, Copilot or GitHub Copilot, or Microsoft Fabric is highly recommended.

# Experience

## SQL Server experience

- Prior experience working with SQL Server as a database administrator, developer, or data engineer
- Exposure to database administration, security, performance tuning, or availability features is helpful but not required

## Azure and Microsoft Fabric

- Deep Azure expertise is not required. However, attendees should be comfortable navigating the Azure portal; provisioning, configuring, and managing resources; and understanding resource permissions.
- Deep Microsoft Fabric expertise is not required. However, attendees should understand core Fabric concepts and data analytics fundamentals.

# Tools and software

- Bring a Windows laptop configured as described below:

| Category                                   | Requirements                                                                                                                                                                                                                                                                                                                                                           |
| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Fundamental exercises — *Minimum required* | Install SQL Server Management Studio v22.10 (released on September 15, 2026) or later with the Hybrid and Migration workload. Ensure that you can connect to a local or workshop-provided SQL Server instance and an Azure SQL database. Details are provided below.                                                                                                   |
| MFA on your phone — *Minimum required*     | Install the Android or iOS version of the Microsoft Authenticator app on your phone. We will provide Azure sign-in accounts on the day of the tutorial. The Microsoft Authenticator app may be required to complete multi-factor authentication (MFA) when signing in. We recommend installing the app on your phone in advance.                                       |
| Advanced exercises — *Highly recommended*  | Install Visual Studio Code; install the MSSQL extension for Visual Studio Code; set up GitHub Copilot in Visual Studio Code and configure it with your personal account, if available; set up the SQL MCP Server in Visual Studio Code; set up the Azure MCP Server in Visual Studio Code; install .NET SDK 10 on your machine; and install Azure CLI on your machine. |
| Optional software — *Expert-level*         | Install GitHub Desktop and Python 3.x.                                                                                                                                                                                                                                                                                                                                 |

- Note: If you can only bring a macOS laptop, please be aware that SSMS runs only on Windows. You can use the MSSQL extension for Visual Studio Code or another SQL Server management tool, but we will not be able to provide support for these tools, and you will not be able to execute several exercises using SSMS features during the workshop.

> [!IMPORTANT]
> - If your work laptop has these restrictions, consider bringing a personal laptop.

Before arriving, verify that your laptop can access the required online services and connect to the workshop databases by completing the checks below.

# Fundamental Exercises

Complete the following checks to participate in the fundamental hands-on exercises.

## Install the SSMS Hybrid and Migration workload

The Hybrid and Migration workload is the required SSMS component for running and viewing database assessments in this workshop.

1. Close SSMS if it is open.
2. Open **Visual Studio Installer** from the Windows Start menu.
3. Locate **SQL Server Management Studio 22**, and then select **Modify**.
4. On the **Workloads** tab, select Hybrid and Migration.
5. Select **Install while downloading**, and then select **Modify**.
6. When the installation finishes, select **Launch**.
7. Connect to a SQL Server instance in SSMS, right-click the top-level server node in **Object Explorer**, and confirm that **Migrate SQL Server** is available.

If **Migrate SQL Server** is already available, the workload is installed and no modification is required.

> [!NOTE]
>
> Modifying SSMS requires local administrator permissions. If you cannot install the workload before the workshop, you can still complete the Managed Instance link exercise, but you must complete the optional database assessment later on a computer that has the workload installed.

## Azure Portal access

- For the workshop, we will provide access to a temporary Azure subscription and issue temporary credentials on the day of the event.
- Ensure that you do not have any restrictions on your laptop to access [https://portal.azure.com](https://portal.azure.com)
- If clicking on this link works from your laptop, you are all set.

## SQL Server access

- For the workshop, we are providing access to a temporary SQL Server instance.
- Before arriving, use SSMS and the following settings to verify that you can connect
- Your connection details will be provided to you directly. 
- Note: At the workshop, you will be provided different credentials with elevated permissions to perform the exercises.

# Advanced Exercises

The following setup is optional but highly recommended for participants who want to complete the advanced hands-on exercises. If you cannot finish any of these steps before the workshop, you may be unable to perform the corresponding exercises, but you can still follow the demonstrations on the projected screen.

## Configure GitHub Copilot account

- For the purpose of this workshop, we are providing GitHub Copilot account credits that will expire after the workshop. Configuration steps will be provided directly to you.
- If you or your organization already provides a GitHub Copilot subscription, you may skip this step.

Next:

- Configure SSMS on your laptop with your GitHub account
  - Test GitHub Copilot with a simple prompt, such as “Are you running? Which version of SSMS am I using?” Confirm that it responds successfully.
- Configure Visual Studio Code with the GitHub Copilot extension and sign in with this account.
  - Test GitHub Copilot with a simple prompt, such as “Are you running? Which version of Visual Studio Code am I using?” Confirm that it responds successfully.

Alternatively, you may skip the workshop sign-up and use a corporate account that already includes Copilot credits.

> [!IMPORTANT]
> To participate in the advanced exercises, configure GitHub Copilot in SSMS and Visual Studio Code before arriving at the workshop.

## Validate: Azure CLI installed

For advanced exercises, ensure you can run the Azure CLI from your machine’s command prompt.

- From the prompt of the laptop you will bring to the workshop, type:

```plaintext
az version
```

- The command should return the Azure CLI version
- If the command returns an error, install [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest&pivots=winget) on your laptop and run the validation again.

## Validate: .NET SDK 10 installed

- To run the Zava demo application during the advanced exercises, ensure that .NET SDK 10 is installed on your laptop.
- You can use a [Windows installer](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks), or use the following commands from your machine’s prompt:

```plaintext
winget install --id Microsoft.DotNet.SDK.10 --exact
winget install --id Microsoft.AzureCLI --exact
winget install --id Microsoft.VisualStudioCode --exact
```

- You will be provided with the Zava demo app source code at a later time. If you are unable to complete this requirement now, you can complete it on your own at home later.

# Before you arrive at the workshop

- Review the pre-event setup instructions provided in this document.
- Validate all tool installations outlined in this document before arriving.

# Questions?

- If you have any questions, please reach out via contacts provided in the email.
- Please send questions as early as possible, as responses may not be immediate.

# What's next

Congratulations! 🎉

You prepared your Windows laptop for the workshop by installing SSMS with the Hybrid and Migration workload, confirming access to the Azure portal and workshop databases, and reviewing the required technical knowledge. If you completed the advanced setup, you also validated GitHub Copilot, Visual Studio Code, Azure CLI, and .NET SDK 10.

Next, continue to [Module 1 - Introduction and Setup](../Module%201%20-%20Introduction%20and%20Setup/README.md).
