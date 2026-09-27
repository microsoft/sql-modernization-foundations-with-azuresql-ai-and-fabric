# Exercise 3.1 - SQL Managed Instance Benefits

In Module 2, you moved the ZavaFin database from SQL Server on premises to Azure SQL Managed Instance. Migration changed where the workload runs. Modernization changes how the workload is operated, protected, and extended.

This short exercise reviews the operational value of that move before you add AI capabilities. You will not deploy database objects or change the application.

## Prerequisites

- Complete [Exercise 3.0 - About ZavaFin](<../00 - About ZavaFin/README.md>).
- Complete the [Module 2 migration](<../../Module 2 - Migrate to Azure/README.md>).

## What you will learn

By the end of this exercise, you will be able to:

- explain the main Managed Instance modernization benefits; and
- explain why Managed Instance is a foundation for the AI capabilities in the rest of this module.

## Benefits of SQL Managed Instance

Managed Instance combines broad SQL Server compatibility with a managed Azure platform. ZavaFin keeps familiar databases, T-SQL, stored procedures, application contracts, and tools while reducing infrastructure work and adding cloud capabilities incrementally.

| Benefit                                      | Value for ZavaFin                                                                                                              |
| -------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| **Scalability**                              | Compute and storage can be adjusted as demand changes, without procuring new hardware.                                         |
| **Security**                                 | Encryption, masking, auditing, private networking, and Microsoft Entra authentication help protect lending data.               |
| **High availability and reliability**        | Built-in redundancy and automatic failover protect against many infrastructure and service events.                             |
| **Reduced management overhead**              | Microsoft operates the infrastructure and automates backups, patching, and availability management.                            |
| **Cost optimization**                        | Elastic resources and managed operations can reduce overprovisioning and infrastructure effort.                                |
| **SQL Server compatibility**                 | Familiar T-SQL and broad engine compatibility reduce application changes during migration and modernization.                   |
| **Automated backup and long-term retention** | Automated backups provide point-in-time restore, with long-term retention available for compliance and recovery requirements.  |
| **Managed platform lifecycle**               | Microsoft patches and evolves the service, reducing recurring infrastructure upgrade projects and adding managed capabilities. |

## Why this matters for ZavaFin

The rest of this module depends on three properties of the new platform:

1. **Compatibility preserves application contracts.** The application continues to use familiar SQL connectivity and stored procedures.
2. **Managed operations create capacity for higher-value work.** The team can focus more on search, decision support, governance, and customer outcomes.
3. **Azure proximity enables incremental innovation.** SQL can use Azure identity, networking, monitoring, and AI services while operational data remains governed in the database.

Next, you will add embeddings, vector search, hybrid retrieval, and grounded AI-assisted scoring while the application contracts remain stable.

## Learn more

- [What is Azure SQL Managed Instance?](https://learn.microsoft.com/azure/azure-sql/managed-instance/sql-managed-instance-paas-overview?view=azuresql)
- [Automatic, geo-redundant backups](https://learn.microsoft.com/azure/azure-sql/managed-instance/automated-backups-overview?view=azuresql)
- [Availability through local and zone redundancy](https://learn.microsoft.com/azure/azure-sql/managed-instance/high-availability-sla-local-zone-redundancy?view=azuresql)

## Exercise summary

You established that:

- migration moved ZavaFin's database, while modernization improves how it is operated and extended;
- Managed Instance combines SQL Server compatibility with scalability, security, availability, managed backups, and a managed platform lifecycle;
- the managed Azure platform provides the foundation for the AI exercises that follow.

Continue to [Exercise 3.2 - AI Foundations](<../02 - AI Foundations/README.md>).
