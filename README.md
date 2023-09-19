---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
  services: Sql
  platforms: dotnet
---

# Getting started on managing SQL failover groups in C# #

 Azure SQL sample for managing SQL Failover Groups
  - Create a primary SQL Server with a sample database and a secondary SQL Server.
  - Get a failover group from the primary SQL server to the secondary SQL server.
  - Update a failover group.
  - List all failover groups.
  - Delete a failover group.
  - Delete Sql Server


## Running this Sample ##

To run this sample:

Set the environment variable `CLIENT_ID`,`CLIENT_SECRET`,`TENANT_ID`,`SUBSCRIPTION_ID` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/sql-database-dotnet-manage-sql-failover-groups.git

    cd sql-database-dotnet-manage-sql-failover-groups

    dotnet build

    bin\Debug\net452\ManageSqlFailoverGroups.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.