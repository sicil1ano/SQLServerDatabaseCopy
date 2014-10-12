using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerDatabaseCopy
{
    /// <summary>
    /// ApplicationSettingsHelper class. It manages the connection string and other settings contained in the config file.
    /// </summary>
    public class ApplicationSettingsHelper
    {
        #region Properties

        public string ConnectionString { get; set; }

        public string CloneDatabaseNameSuffix { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Main Constructor. It initializes the public properties.
        /// </summary>
        public ApplicationSettingsHelper()
        {
            this.ConnectionString = GetConnectionString();
            this.CloneDatabaseNameSuffix = GetCloneDatabaseNameSuffix();
        }

        #endregion

        #region Members

        /// <summary>
        /// Gets a custom connection string according to the database name given.
        /// </summary>
        /// <param name="databaseName">The database name for which a connection string is generated.</param>
        /// <returns>The connection string for the given database.</returns>
        public string GetUserDatabaseConnectionString(string databaseName)
        {
            string connectionString = String.Empty;

            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["SqlServerConnection"];

            if (settings != null)
            {
                connectionString = settings.ConnectionString;
            }

            connectionString = connectionString.Replace("master", databaseName);

            return connectionString;
        }

        /// <summary>
        /// Gets a new clone database name suffix, according to date and time of today.
        /// </summary>
        /// <returns>The new clone database name suffix.</returns>
        public string GetNewCloneDatabaseNameSuffix()
        {
            string newSuffix = "_" + System.Text.RegularExpressions.Regex.Replace(DateTime.Now.ToString(), @"[^\w\.@-]", ""); 
            return newSuffix;
        }

        /// <summary>
        /// Gets the connection string set in the configuration file of the applications.
        /// </summary>
        /// <returns>The connection string configured in the config file.</returns>
        private string GetConnectionString()
        {
            string connectionString = String.Empty;

            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["SqlServerConnection"];

            if (settings != null)
            {
                connectionString = settings.ConnectionString;
            }

            return connectionString;
        }

        /// <summary>
        /// Gets the customized clone database name suffix to add to the name of each database to copy.
        /// </summary>
        /// <returns>The suffix to add to the name of each database to copy.</returns>
        private string GetCloneDatabaseNameSuffix()
        {
            string clonedDatabaseName = String.Empty;

            var clonedDatabaseSetting = ConfigurationManager.AppSettings["CloneDatabaseNameSuffix"];

            if (clonedDatabaseSetting != null)
            {
                clonedDatabaseName = clonedDatabaseSetting;
            }

            return clonedDatabaseName;
        }

        #endregion
    }
}
