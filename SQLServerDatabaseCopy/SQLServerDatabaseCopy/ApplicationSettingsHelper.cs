using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerDatabaseCopy
{
    /// <summary>
    /// ApplicationSettingsHelper class. It manages the connection string contained in the config file.
    /// </summary>
    public static class ApplicationSettingsHelper
    {
        /// <summary>
        /// Gets the connection string set in the configuration file of the applications.
        /// </summary>
        /// <returns>The connection string configured in the config file.</returns>
        public static string GetConnectionString()
        {
            string connectionString = null;

            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["SqlServerConnection"];

            if (settings != null)
            {
                connectionString = settings.ConnectionString;
            }

            return connectionString;
        }

        /// <summary>
        /// Gets a custom connection string according to the database name given.
        /// </summary>
        /// <param name="databaseName">The database name for which a connection string is generated.</param>
        /// <returns>The connection string for the given database.</returns>
        public static string GetUserDatabaseConnectionString(string databaseName)
        {
            string connectionString = null;

            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["SqlServerConnection"];

            if (settings != null)
            {
                connectionString = settings.ConnectionString;
            }

            connectionString = connectionString.Replace("master", databaseName);

            return connectionString;
        }

        public static string GetClonedDatabaseNameSuffix()
        {
            string clonedDatabaseName = null;

            var clonedDatabaseSetting = ConfigurationManager.AppSettings["CloneDatabaseNameSuffix"];

            if(clonedDatabaseSetting != null)
            {
                clonedDatabaseName = clonedDatabaseSetting;
            }

            return clonedDatabaseName;
        }
    }
}
