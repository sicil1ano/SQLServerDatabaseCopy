using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLServerDatabaseCopy
{
    /// <summary>
    /// LogFileManager class. It is used to manage log files within the application.
    /// </summary>
    public class LogFileManager
    {
        #region Fields

        private const string tempLogFileName = "SMOTestTempLog";
        private const string logFileName = "SMOTestLog_";
        private const string logFileExt = ".txt";
        private const string logFileDirectory = @"C:\Log";

        private string tempLogFilePath = Path.Combine(logFileDirectory, tempLogFileName+logFileExt);

        #endregion

        #region Constructors

        /// <summary>
        /// Main Constructor. It creates a temporary log file.
        /// </summary>
        public LogFileManager()
        {
            CreateTempLogFile();
        }

        #endregion

        #region Members

        /// <summary>
        /// Creates a temporary log file.
        /// </summary>
        private void CreateTempLogFile()
        {
            if (!Directory.Exists(logFileDirectory))
            {
                Directory.CreateDirectory(logFileDirectory);
            }

            if (!File.Exists(tempLogFilePath))
            {
                using (StreamWriter swTempFile = new StreamWriter(tempLogFilePath))
                {
                    swTempFile.WriteLine("Created Log File.");
                    swTempFile.WriteLine("Registered data started at " + DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"));
                    swTempFile.WriteLine("OUTPUT LOG:");
                    swTempFile.Flush();
                }
            }
        }

        /// <summary>
        /// Writes a string to the log file.
        /// </summary>
        /// <param name="output">The string to write.</param>
        public void WriteToLogFile(string output = "")
        {
            if (File.Exists(tempLogFilePath))
            {
                using (StreamWriter swTempFile = new StreamWriter(tempLogFilePath, true))
                {
                    swTempFile.WriteLine(output);
                    swTempFile.Flush();
                }
            }
        }

        /// <summary>
        /// Closes the log file.
        /// </summary>
        public void CloseLogFile()
        {
            using (StreamWriter swTempFile = new StreamWriter(tempLogFilePath, true))
            {
                swTempFile.WriteLine("YEAH");
                swTempFile.WriteLine("Registered data terminated at " + DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"));
                swTempFile.WriteLine("END OF LOG FILE");
                swTempFile.Flush();
            }

            CreateFinalLogFile();
            DeleteTempLogFile();
        }

        /// <summary>
        /// Creates the final log file at the end of temporary log file closing.
        /// </summary>
        private void CreateFinalLogFile()
        { 
            string newLogFilePath = Path.Combine(logFileDirectory,logFileName+DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss")+logFileExt);
            File.Copy(tempLogFilePath, newLogFilePath);
        }

        /// <summary>
        /// Deletes the temporary log file.
        /// </summary>
        private void DeleteTempLogFile()
        {
            if (File.Exists(tempLogFilePath))
            {
                File.Delete(tempLogFilePath);
            }
        }

        #endregion
    }
}
