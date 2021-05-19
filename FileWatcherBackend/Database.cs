using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWatcherBackend
{
    public static class Database
    {
        private static SqlConnection _connection;
        private static SqlConnectionStringBuilder _csb;
        public static string Datasource
        {
            get { return _csb.DataSource; }
        }
        public static SqlConnection Connection
        {
            get
            {
                if (_connection.State == ConnectionState.Broken)
                {
                    Logger.Log("Connection to database server " + _csb.DataSource + " was in a broken state, reopening...", System.Diagnostics.EventLogEntryType.Warning);
                    _connection.Close();
                    _connection.Open();
                }
                else if (_connection.State == ConnectionState.Closed)
                {
                    Logger.Log("Opening connection to database server...");
                    _connection.Open();
                }
                return _connection;
            }
        }
        public static void InitConnection(string datasource)
        {
            _csb = new SqlConnectionStringBuilder();

            _csb.DataSource = datasource;
            _csb.InitialCatalog = "msdb";
            _csb.IntegratedSecurity = true;
            _csb.ApplicationName = "File Watcher Service";
            _connection = new SqlConnection(_csb.ToString());

            Logger.Log("Database connection string: \"" + _csb.ToString() + "\"");
        }

        public static void Close()
        {
            Logger.Log("Closing connection to database server...");
            _connection.Close();
        }

        private static bool JobIdExists(string jobId)
        {
            SqlCommand cmd = new SqlCommand("SELECT [job_id] FROM [msdb].[dbo].[sysjobs] WHERE [job_id] = @job_id", Connection);
            cmd.CommandType = CommandType.Text;

            SqlParameter pJobId = cmd.Parameters.Add("@job_id", SqlDbType.NVarChar, 255);
            pJobId.Direction = ParameterDirection.Input;
            pJobId.Value = jobId;

            SqlDataReader reader = cmd.ExecuteReader();

            bool hasRows = reader.HasRows;

            reader.Close();

            if (!hasRows)
                return false;

            return true;
        }
        public static SqlAgentJob CreateSqlAgentJobHandler(string jobId)
        {
            if (!JobIdExists(jobId))
                return null;

            return new SqlAgentJob(jobId);
        }

    }
}
