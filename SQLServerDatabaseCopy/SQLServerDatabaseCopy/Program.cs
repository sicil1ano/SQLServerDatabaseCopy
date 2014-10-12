using System;

namespace SQLServerDatabaseCopy
{
    /// <summary>
    /// Main program class.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("The application will start..");
                ApplicationSettingsHelper ash = new ApplicationSettingsHelper();
                LogFileManager lfm = new LogFileManager();
                DatabaseSchemaManager dbMng = new DatabaseSchemaManager(ash,lfm);
                dbMng.Execute();
                lfm.CloseLogFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception occurred. Message: {0}", ex.Message);
                Console.WriteLine("Exception StackTrace: {0}", ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("The application has completed. Press any key to exit..");

            Console.ReadKey();
        }
    }
}
