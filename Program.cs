// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using System;

namespace ManageSqlFailoverGroups
{
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;

        /**
         * Azure SQL sample for managing SQL Failover Groups
         *  - Create a primary SQL Server with a sample database and a secondary SQL Server.
         *  - Get a failover group from the primary SQL server to the secondary SQL server.
         *  - Update a failover group.
         *  - List all failover groups.
         *  - Delete a failover group.
         *  - Delete Sql Server
         */
        public static async Task RunSample(ArmClient client)
        {
            try
            {
                // ============================================================
                //Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                //Create a resource group in the EastUS region
                string rgName = Utilities.CreateRandomName("rgSQLServer");
                Utilities.Log("Creating resource group...");
                var rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log($"Created a resource group with name: {resourceGroup.Data.Name} ");

                // ============================================================
                // Create a primary SQL Server with a sample database.
                Utilities.Log("Creating a primary SQL Server with a sample database");
                string sqlPrimaryServerName = Utilities.CreateRandomName("sqlserver-failovertest");
                Utilities.Log("Creating primary SQL Server...");
                string sqlAdmin = "sqladmin1234";
                string sqlAdminPwd = Utilities.CreatePassword();
                SqlServerData sqlPrimaryServerData = new SqlServerData(AzureLocation.SoutheastAsia)
                {
                    AdministratorLogin = sqlAdmin,
                    AdministratorLoginPassword = sqlAdminPwd
                };
                var sqlPrimaryServer = (await resourceGroup.GetSqlServers().CreateOrUpdateAsync(WaitUntil.Completed, sqlPrimaryServerName, sqlPrimaryServerData)).Value;
                Utilities.Log($"Created primary SQL Server with name: {sqlPrimaryServer.Data.Name} ");

                Utilities.Log("Creating a database in primary SQL Server...");
                string sqlPrimaryDBName = Utilities.CreateRandomName("SQLPrimaryDB");
                SqlDatabaseData sqlPrimaryDBData = new SqlDatabaseData(AzureLocation.SoutheastAsia)
                {
                    Sku = new SqlSku("S0")
                    {
                        Tier = "Standard"
                    }
                };
                SqlDatabaseResource sqlPrimaryDB = (await sqlPrimaryServer.GetSqlDatabases().CreateOrUpdateAsync(WaitUntil.Completed, sqlPrimaryDBName, sqlPrimaryDBData)).Value;
                Utilities.Log($"Created a database in primary SQL Server with name: {sqlPrimaryDB.Data.Name} ");

                // ============================================================
                // Create a secondary SQL Server with a sample database.
                Utilities.Log("Creating a secondary SQL Server with a sample database");
                string sqlSecondaryServerName = Utilities.CreateRandomName("sqlserver-failovertest");
                Utilities.Log("Creating secondary SQL Server...");
                SqlServerData sqlSecondaryServerData = new SqlServerData(AzureLocation.EastUS2)
                {
                    AdministratorLogin = sqlAdmin,
                    AdministratorLoginPassword = sqlAdminPwd
                };
                var sqlSecondaryServer = (await resourceGroup.GetSqlServers().CreateOrUpdateAsync(WaitUntil.Completed, sqlSecondaryServerName, sqlSecondaryServerData)).Value;
                Utilities.Log($"Created secondary SQL Server with name: {sqlSecondaryServer.Data.Name} ");

                //Utilities.Log("Creating a database in secondary SQL Server...");
                //string sqlSecondaryDBName = Utilities.CreateRandomName("SQLSecondaryDB");
                //SqlDatabaseData sqlSecondaryDBData = new SqlDatabaseData(AzureLocation.EastUS2)
                //{
                //    Sku = new SqlSku("S0")
                //    {
                //        Tier = "Standard"
                //    }
                //};
                //SqlDatabaseResource sqlSecondaryDB = (await sqlSecondaryServer.GetSqlDatabases().CreateOrUpdateAsync(WaitUntil.Completed, sqlSecondaryDBName, sqlSecondaryDBData)).Value;
                //Utilities.Log($"Created a database in secondary SQL Server with name: {sqlSecondaryDB.Data.Name} ");

                // ============================================================
                // Create a Failover Group from the primary SQL server to the secondary SQL server.
                Utilities.Log("Creating a Failover Group from the primary SQL server to the secondary SQL server");
                string failoverGroupName = Utilities.CreateRandomName("my-other-failover-group");
                var failoverGroupData = new FailoverGroupData()
                {
                    ReadWriteEndpoint = new FailoverGroupReadWriteEndpoint(ReadWriteEndpointFailoverPolicy.Manual),
                    ReadOnlyEndpointFailoverPolicy = ReadOnlyEndpointFailoverPolicy.Disabled,
                    PartnerServers =
                    {
                        new PartnerServerInfo(sqlSecondaryServer.Id)
                    }
                };
                var failoverGroup = (await sqlPrimaryServer.GetFailoverGroups().CreateOrUpdateAsync(WaitUntil.Completed,failoverGroupName,failoverGroupData)).Value;

                Utilities.Log($"Created a Failover Group with name {failoverGroup.Data.Name}");

                // ============================================================
                // Get the Failover Group from the secondary SQL server.
                Utilities.Log("Getting the Failover Group from the secondary SQL server...");

                var failoverGroupOnPartner = await sqlSecondaryServer.GetFailoverGroupAsync(failoverGroup.Data.Name);

                Utilities.Log($"Get the Failover Group from the secondary SQL server with name: {failoverGroupOnPartner.Value.Data.Name}");

                // ============================================================
                // Update the Failover Group Endpoint policies and tags.
                Utilities.Log("Updating the Failover Group Endpoint policies and tags...");
                var updateData = new FailoverGroupPatch()
                {
                    ReadWriteEndpoint = new FailoverGroupReadWriteEndpoint(ReadWriteEndpointFailoverPolicy.Automatic)
                    {
                        FailoverWithDataLossGracePeriodMinutes = 120
                    },
                    Tags =
                    {
                        ["tag1"]="value1",
                        ["tag2"]="update-test"
                    },
                    ReadOnlyEndpointFailoverPolicy = ReadOnlyEndpointFailoverPolicy.Enabled
                };
                failoverGroup = (await failoverGroup.UpdateAsync(WaitUntil.Completed,updateData)).Value;

                Utilities.Log($"Updated the Failover Group Endpoint policies and tags, the policies is {failoverGroup.Data.ReadOnlyEndpointFailoverPolicy} and {failoverGroup.Data.ReadWriteEndpoint.FailoverPolicy}");

                // ============================================================
                // Update the Failover Group to add database and change read-write endpoint's failover policy.
                Utilities.Log("Updating the Failover Group to add database and change read-write endpoint's failover policy...");

                var addDBData = new FailoverGroupPatch()
                {
                    ReadWriteEndpoint = new FailoverGroupReadWriteEndpoint(ReadWriteEndpointFailoverPolicy.Manual),
                    ReadOnlyEndpointFailoverPolicy = ReadOnlyEndpointFailoverPolicy.Disabled,
                    Databases =
                    {
                        sqlPrimaryDB.Data.Id
                    }
                };
                failoverGroup = (await failoverGroup.UpdateAsync(WaitUntil.Completed,addDBData)).Value;

                Utilities.Log($"Updated the Failover Group to add database and change read-write endpoint's failover policy with Endpoint policies: {failoverGroup.Data.ReadOnlyEndpointFailoverPolicy}, {failoverGroup.Data.ReadWriteEndpoint.FailoverPolicy}");

                // ============================================================
                // List the Failover Group on the secondary server.
                Utilities.Log("Listing the Failover Group on the secondary server...");

                foreach (var item in sqlSecondaryServer.GetFailoverGroups().ToList())
                {
                    Utilities.Log($"The Failover Group with name: {item.Data.Name} on the secondary server");
                }

                // ============================================================
                // Get the database from the secondary SQL server.
                Utilities.Log("Getting the database from the secondary server...");
                Thread.Sleep(TimeSpan.FromMinutes(3));

                sqlPrimaryDB = (await sqlSecondaryServer.GetSqlDatabaseAsync(sqlPrimaryDB.Data.Name));

                Utilities.Log($"Get the database from the secondary server with databasename: {sqlPrimaryDB.Data.Name}");

                // ============================================================
                // Delete the Failover Group.
                Utilities.Log("Deleting the Failover Group...");

                await failoverGroup.DeleteAsync(WaitUntil.Completed);

                // Delete the SQL Servers.
                Utilities.Log("Deleting the Sql Servers...");
                await sqlPrimaryServer.DeleteAsync(WaitUntil.Completed);
                await sqlSecondaryServer.DeleteAsync(WaitUntil.Completed);
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group...");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId.Name}");
                    }
                }
                catch (Exception e)
                {
                    Utilities.Log(e);
                }
            }
        }
        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e.ToString());
            }
        }
    }
}