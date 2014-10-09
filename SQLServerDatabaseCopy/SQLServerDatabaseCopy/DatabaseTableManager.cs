using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace SQLServerDatabaseCopy
{
    /// <summary>
    /// DatabaseTableManager class. It manages the data copying process from source tables to destination tables.
    /// </summary>
    public class DatabaseTableManager
    {
        #region Properties

        public LogFileManager LogFileManager { get; set; }
        public Database SourceDatabase { get; set; }
        public string ConnectionString { get; set; }

        private TableCollection Tables
        {
            get
            {
                return SourceDatabase.Tables;
            }
        }

        #endregion

        #region Constructors

        #endregion

        #region Members

        /// <summary>
        /// Gets the list of columns for the given table.
        /// </summary>
        /// <param name="table">The table for which we need to find the list of columns.</param>
        /// <returns>The list of columns.</returns>
        private string GetListOfColumnsOfTable(Table table)
        {
            StringBuilder tables = new StringBuilder();
            string columnName = String.Empty;

            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (!table.Columns[i].Computed)
                {
                    columnName = String.Format("[{0}]", table.Columns[i].Name);
                    tables.Append(columnName);
                    if (i != (table.Columns.Count - 1))
                    {
                        tables.Append(',');
                    }
                }
            }

            return tables.ToString();
        }

        /// <summary>
        /// Checks if the given table has the IDENTITY PROPERTY enabled. (NOT USED)
        /// </summary>
        /// <param name="connection">The connection to the database for the given table.</param>
        /// <param name="table">The table to check.</param>
        /// <returns></returns>
        private bool TableHasIdentityProperty(SqlConnection connection, Table table)
        {
            bool result = false;
            string checkIdentityStatement = "select name " +
                                            "from sysobjects " +
                                            "where xtype = 'U' and name = '{0}' " +
                                            "and OBJECTPROPERTY(id, 'TableHasIdentity') = 1";

            checkIdentityStatement = String.Format(checkIdentityStatement, table.Name);
            SqlCommand checkIdentityCommand = new SqlCommand(checkIdentityStatement, connection);

            try
            {
                SqlDataReader reader = checkIdentityCommand.ExecuteReader();
                if (reader.Read())
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown with message : {0}", ex.Message);
                Console.WriteLine("Stacktrace: {0}", ex.StackTrace);
            }

            return result;
        }

        /// <summary>
        /// Manages the bulk data copying process from each source table to each destination table.
        /// </summary>
        /// <param name="cloneDatabase">The clone database for which we need to perform the bulk data copying process.</param>
        public void CopyDataFromTableToTable(Database cloneDatabase)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }

                using (SqlCommand useCommand = new SqlCommand("USE [" + SourceDatabase.Name + "]", connection))
                {
                    LogFileManager.WriteToLogFile(useCommand.CommandText);
                    useCommand.ExecuteNonQuery();
                }

                string destDatabaseConnString = ApplicationSettingsHelper.GetUserDatabaseConnectionString(cloneDatabase.Name);

                Console.WriteLine("Starting copying data from {0} source tables..", Tables.Count);
                LogFileManager.WriteToLogFile(String.Format("Starting copying data from {0} source tables..", Tables.Count));

                foreach (Table table in Tables)
                {
                    string columnsTable = GetListOfColumnsOfTable(table);

                    string bulkCopyStatement = "SELECT {3} FROM [{0}].[{1}].[{2}]";
                    bulkCopyStatement = String.Format(bulkCopyStatement, SourceDatabase.Name, table.Schema, table.Name, columnsTable);

                    using (SqlCommand selectCommand = new SqlCommand(bulkCopyStatement, connection))
                    {
                        LogFileManager.WriteToLogFile(bulkCopyStatement);
                        SqlDataReader dataReader = selectCommand.ExecuteReader();

                        using (SqlConnection destinationDatabaseConnection = new SqlConnection(destDatabaseConnString))
                        {
                            if (destinationDatabaseConnection.State == System.Data.ConnectionState.Closed)
                            {
                                destinationDatabaseConnection.Open();
                            }

                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(destinationDatabaseConnection))
                            {
                                bulkCopy.DestinationTableName = String.Format("[{0}].[{1}]", table.Schema, table.Name);

                                foreach (Column column in table.Columns)
                                {
                                    //it's not needed to perfom a mapping for computed columns!
                                    if (!column.Computed)
                                    {
                                        bulkCopy.ColumnMappings.Add(column.Name, column.Name);
                                    }
                                }

                                try
                                {
                                    bulkCopy.WriteToServer(dataReader);
                                    LogFileManager.WriteToLogFile(String.Format("Bulk copy successful for table [{0}].[{1}]", table.Schema, table.Name));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                                finally
                                {
                                    //closing reader
                                    dataReader.Close();
                                }
                            }
                        }
                    }
                }
                //closing main connection
                connection.Close();
            }

            Console.WriteLine("Successfully copied data from source tables to the new tables.");
            LogFileManager.WriteToLogFile("Successfully copied data from source tables to the new tables.");
        }

        #endregion
    }
}
