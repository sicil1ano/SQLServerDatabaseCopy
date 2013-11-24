SQLServerDatabaseCopy
=====================

A console application that performs the copy of of each SQL Server user database in the selected instance.
The connection string needed to connect to the selected SQL Server instance is set in the *.config file of the application.

The database copy is performed generating the schema of each user database and executing a bulk copy of the data contained in each table of the each user database.

The application, furthermore, writes a log file (with *.txt extension) to monitor the status of each operation involved in the copy.


The application uses SMO (SQL Server Management Objects) libraries : http://technet.microsoft.com/en-us/library/ms162169.aspx
