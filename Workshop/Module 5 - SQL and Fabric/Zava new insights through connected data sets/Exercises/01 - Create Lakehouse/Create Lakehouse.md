# Create Lakehouse to connect two data sources and generate actionable insights

## About the Lakehouse in Fabric

A Lakehouse in Microsoft Fabric is a unified data platform that combines the scalability and low-cost storage of a data lake with the structured querying and management capabilities of a data warehouse.

It stores data in open Delta Lake format within OneLake and enables users to work with the same data using SQL, Spark, notebooks, data pipelines, machine learning, and Power BI.

Organizations typically use Lakehouses when they need to consolidate data from multiple sources, perform large-scale data engineering and analytics, build AI and machine learning solutions, or create a shared foundation for reporting and business intelligence across teams.

## Step by step instructions to create Lakehouse

Login to Fabric portal, find your workspace and follow step by step instructions:

1. Select New item, search for "Lakehouse" and select it.

<img src="./images/pasted_20260924-164352.png" width="800" />

2. Get data to your Lakehouse. In our case we will create **shortcuts **towards the data already in One Lake

<img src="./images/pasted_20260924-164422.png" width="800" />

3. Select One Lake from the list of sources

<img src="./images/pasted_20260924-164521.png" width="800" />

4. Next step is to select data source.

<img src="./images/pasted_20260924-164600.png" width="800" />

5. Choose **Passthrough identity** connection model.

<img src="./images/pasted_20260924-164637.png" width="800" />

6. Select tables as shown on the picture (**Applicants**, **LoanHistory** and **LoanHistoryExpanded**)

<img src="./images/pasted_20260924-164722.png" width="800" />

7. Finally, confirm shortcut creation.

<img src="./images/pasted_20260924-164756.png" width="800" />

In the above steps we demonstrated data mirrored from **ZavaLendingDB **database, but step has to be repeated for **ZavaDigitalAssitant** (select **ChatSessions**, **ChatMessages** and **ChatEscalations** tables)

One setup is finished, you can explore state of your Lakehouse in **Explorer**:

<img src="./images/pasted_20260924-165238.png" width="800" />
