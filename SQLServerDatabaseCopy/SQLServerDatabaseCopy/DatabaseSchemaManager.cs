﻿using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace SQLServerDatabaseCopy
{
    /// <summary>
    /// DatabaseSchemaManager class. It manages the cloning of databases and related schemas.
    /// </summary>
    public class DatabaseSchemaManager
    {
        #region Properties

        public LogFileManager LogFileManager { get; set; }
        public Server Server { get; set; }
        public string ConnectionString { get; set; }
        public string CloneDatabaseNameSuffix { get; set; }

        private DatabaseCollection Databases
        {
            get
            {
                return Server.Databases;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Main Constructor. It initialises a new server connection, using the connection string set in the config file of application.
        /// </summary>
        public DatabaseSchemaManager()
        {
            this.ConnectionString = ApplicationSettingsHelper.GetConnectionString();
            this.CloneDatabaseNameSuffix = ApplicationSettingsHelper.GetCloneDatabaseNameSuffix();
            var sqlConnection = new SqlConnection(this.ConnectionString);
            var serverConnection = new ServerConnection(sqlConnection);
            this.Server = new Server(serverConnection);
        }

        #endregion

        #region Members

        /// <summary>
        /// Gets a list of user databases for the selected instance.
        /// </summary>
        /// <returns>List of User Databases.</returns>
        public List<Database> GetUserDatabases()
        {
            LogFileManager.WriteToLogFile(String.Format("Using the Connection String: {0}", ConnectionString));
            Console.WriteLine("Getting the list of user databases installed in the selected server..");
            LogFileManager.WriteToLogFile("Getting the list of user databases installed in the selected server..");

            DatabaseCollection allDatabasesCollection = Databases;
            List<Database> userDatabasesCollection = new List<Database>();

            foreach (Database database in allDatabasesCollection)
            {
                //only user databases are needed!
                if (!database.IsSystemObject)
                {
                    userDatabasesCollection.Add(database);
                }
            }
            Console.WriteLine("The list of user databases installed in the selected server is retrieved.");
            LogFileManager.WriteToLogFile("The list of user databases installed in the selected server is retrieved.");

            return userDatabasesCollection;
        }

        /// <summary>
        /// Generates the database schema, starting from a source user database.
        /// </summary>
        /// <param name="userDatabase">The source user database needed to generate the schema.</param>
        /// <param name="cloneDatabase">The destination user database for which the schema will be transfered.</param>
        /// <returns></returns>
        private StringCollection GenerateDatabaseSchema(Database userDatabase, Database cloneDatabase)
        {
            //Console.WriteLine(String.Format("Starting schema generation of database {0} to be copied for the cloned database.", userDatabase.Name));
            LogFileManager.WriteToLogFile(String.Format("Starting schema generation of database {0} to be copied for the cloned database.", userDatabase.Name));
            Transfer transfer = new Transfer(userDatabase);

            transfer.CopySchema = true;
            transfer.CopyAllObjects = true;
            transfer.CopyAllStoredProcedures = true;
            transfer.CopyAllDatabaseTriggers = true;
            transfer.CopyAllTables = true;
            transfer.CopyAllDefaults = true;
            transfer.CopyAllSchemas = true;
            transfer.CopyAllSearchPropertyLists = true;
            transfer.CopyAllViews = true;
            transfer.CopyAllXmlSchemaCollections = true;
            transfer.CopyData = false;

            transfer.Options.Indexes = true;
            transfer.Options.ClusteredIndexes = true;
            transfer.Options.NonClusteredIndexes = true;
            transfer.Options.DriAllConstraints = true;
            transfer.Options.DriAllKeys = true;
            transfer.Options.DriForeignKeys = true;
            transfer.Options.WithDependencies = true;

            transfer.DestinationDatabase = cloneDatabase.Name;

            var sourceDatabaseSchema = transfer.ScriptTransfer();
            //Console.WriteLine(String.Format("Generated schema of database {0} to be copied for the cloned database.", userDatabase.Name));
            LogFileManager.WriteToLogFile(String.Format("Generated schema of database {0} to be copied for the cloned database.", userDatabase.Name));

            return sourceDatabaseSchema;
        }

        /// <summary>
        /// Copies the generated schema from source user database to the destination user database.
        /// </summary>
        /// <param name="cloneDatabase">The destination user database.</param>
        /// <param name="sourceDatabaseSchema">The source user database schema.</param>
        private void CopySchema(Database cloneDatabase, StringCollection sourceDatabaseSchema)
        {
            cloneDatabase.Create();
            Console.WriteLine("Database : " + cloneDatabase.Name + " created.");
            LogFileManager.WriteToLogFile(String.Format("Clone Database : {0} created.", cloneDatabase.Name));
            //Console.WriteLine(String.Format("Starting the schema copying from source database to clone database {0}", cloneDatabase.Name));
            LogFileManager.WriteToLogFile(String.Format("Starting the schema copying from source database to clone database {0}", cloneDatabase.Name));

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand useCommand = new SqlCommand("USE " + cloneDatabase.Name, connection))
                {
                    useCommand.ExecuteNonQuery();
                    LogFileManager.WriteToLogFile(useCommand.CommandText);
                }

                foreach (var scriptLine in sourceDatabaseSchema)
                {
                    using (SqlCommand scriptCommand = new SqlCommand(scriptLine, connection))
                    {
                        int res = scriptCommand.ExecuteNonQuery();
                        LogFileManager.WriteToLogFile(scriptLine);
                    }
                }
                Console.WriteLine(String.Format("Copied schema from source database to the clone database {0}.", cloneDatabase.Name));
                LogFileManager.WriteToLogFile(String.Format("Copied schema from source database to the clone database {0}.", cloneDatabase.Name));

                connection.Close();
            }
        }

        /// <summary>
        /// Perfoms the creation of user database clones from source user databases, copying the related schema and data.
        /// </summary>
        public void ExecuteDatabaseCopy()
        {
            var userDatabases = GetUserDatabases();
            DatabaseTableManager dbtMng = new DatabaseTableManager();
            dbtMng.ConnectionString = this.ConnectionString;
            dbtMng.LogFileManager = this.LogFileManager;
            foreach (var sourceDatabase in userDatabases)
            {
                Database cloneDatabase = new Database(Server, sourceDatabase.Name + CloneDatabaseNameSuffix);
                cloneDatabase.Collation = sourceDatabase.Collation;
                var sourceDatabaseSchema = GenerateDatabaseSchema(sourceDatabase, cloneDatabase);
                CopySchema(cloneDatabase, sourceDatabaseSchema);
                if (cloneDatabase.Tables.Count != 0)
                {
                    dbtMng.SourceDatabase = sourceDatabase;
                    dbtMng.CopyDataFromTableToTable(cloneDatabase);
                }
            }
        }

        #endregion
    }
}
