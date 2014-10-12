using Microsoft.SqlServer.Management.Common;
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
        #region Fields

        bool sqlConnectionIsAvailable;
        Server server;

        #endregion

        #region Properties

        public ApplicationSettingsHelper ApplicationSettingsHelper { get; set; }

        public LogFileManager LogFileManager { get; set; }
        public string ConnectionString { get; set; }
        public string CloneDatabaseNameSuffix { get; set; }

        private DatabaseCollection Databases
        {
            get
            {
                return server.Databases;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Main Constructor. It initialises a new DatabaseSchemaManager object, using the given parameters.
        /// </summary>
        /// <param name="ash">The ApplicationSettingsHelper object to retrieve the application settings.</param>
        /// <param name="lfm">LogFileManager object to write output to a log file.</param>
        public DatabaseSchemaManager(ApplicationSettingsHelper ash, LogFileManager lfm)
        {
            this.ApplicationSettingsHelper = ash;
            this.LogFileManager = lfm;
        }

        #endregion

        #region Members

        /// <summary>
        /// Starts the database copy process.
        /// </summary>
        public void Execute()
        {
            string connectionString = ApplicationSettingsHelper.ConnectionString;
            string cloneDatabaseNameSuffix = ApplicationSettingsHelper.CloneDatabaseNameSuffix;
            InitializeSqlServerConnection(connectionString, cloneDatabaseNameSuffix);
        }

        /// <summary>
        /// Perfoms the creation of user database clones from source user databases, copying the related schema and data.
        /// </summary>
        private void ExecuteDatabaseCopy()
        {
            var userDatabases = GetUserDatabases();
            DatabaseTableManager dbtMng = new DatabaseTableManager();
            dbtMng.ConnectionString = this.ConnectionString;
            dbtMng.LogFileManager = this.LogFileManager;
            dbtMng.ApplicationSettingsHelper = this.ApplicationSettingsHelper;
            foreach (var sourceDatabase in userDatabases)
            {
                Database cloneDatabase = new Database(server, sourceDatabase.Name + CloneDatabaseNameSuffix);
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

        /// <summary>
        /// Gets a list of user databases for the selected instance.
        /// </summary>
        /// <returns>List of User Databases.</returns>
        private List<Database> GetUserDatabases()
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
        /// Initialize the SQL Server Connection using the given parameters.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="cloneDatabaseNameSuffix"></param>
        private void InitializeSqlServerConnection(string connectionString, string cloneDatabaseNameSuffix)
        {
            ValidateConnectionSettings(connectionString, cloneDatabaseNameSuffix);
            if (sqlConnectionIsAvailable)
            {
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                var serverConnection = new ServerConnection(sqlConnection);
                this.server = new Server(serverConnection);
                ExecuteDatabaseCopy();
            }
            else
            {
                Console.WriteLine("Cannot initialize a SQL Server connection using the given connection string {0}", connectionString);
                Console.WriteLine("The process cannot continue. Process aborted.");
                LogFileManager.WriteToLogFile(String.Format("Cannot initialize a SQL Server connection using the given connection string {0}", connectionString));
                LogFileManager.WriteToLogFile("The process cannot continue. Process aborted.");
            }
        }

        /// <summary>
        /// Generates the database schema, starting from a source user database.
        /// </summary>
        /// <param name="userDatabase">The source user database needed to generate the schema.</param>
        /// <param name="cloneDatabase">The destination user database for which the schema will be transfered.</param>
        /// <returns></returns>
        private StringCollection GenerateDatabaseSchema(Database userDatabase, Database cloneDatabase)
        {
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

        private void ValidateConnectionSettings(string connectionString, string cloneDatabaseNameSuffix)
        {
            sqlConnectionIsAvailable = CheckConnectionString(connectionString);
            if (sqlConnectionIsAvailable)
            {
                this.ConnectionString = connectionString;
                if (String.IsNullOrEmpty(cloneDatabaseNameSuffix))
                {
                    this.CloneDatabaseNameSuffix = ApplicationSettingsHelper.GetNewCloneDatabaseNameSuffix();
                }
            }
        }

        /// <summary>
        /// Checks the connection to a SQL Server istance using the given connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use for the connection to a SQL Server istance.</param>
        /// <returns>True if the connection is okay, false otherwise.</returns>
        private bool CheckConnectionString(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (SqlException)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
