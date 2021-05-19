using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWatcherBackend
{
    public enum SqlAgentJobStatus
    {
        NotIdleOrSuspended = 0,
        Executing = 1,
        WaitingForThread = 2,
        BetweenRetries = 3,
        Idle = 4,
        Suspended = 5,
        PerformingCompletitionActions = 7,
        Unknown = 666
    }
    public class SqlAgentJob
    {
        private string _jobId;
        private string _jobName;

        public SqlAgentJob(string jobId)
        {
            this._jobId = jobId;
            //this.FillJobName();
        }
        public SqlAgentJobStatus Status
        {
            get { return GetJobStatus(); }
        }

        public bool Run()
        {
            Logger.Log(this + " running...");
            SqlAgentJobStatus currentJobStatus = GetJobStatus();

            if (currentJobStatus != SqlAgentJobStatus.Idle)
            {
                Logger.Log(this.ToString() + " was not triggered as it was not on idle(4) status. Current status: " + currentJobStatus.ToString() + ".", System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }

            SqlCommand startJobCmd = new SqlCommand("[msdb].[dbo].[sp_start_job]", Database.Connection);
            startJobCmd.CommandType = CommandType.StoredProcedure;

            SqlParameter rc = startJobCmd.Parameters.Add("RetVal", SqlDbType.Int);
            rc.Direction = ParameterDirection.ReturnValue;

            SqlParameter pJobId = startJobCmd.Parameters.Add("@job_id", SqlDbType.NVarChar, 255);
            pJobId.Direction = ParameterDirection.Input;
            pJobId.Value = _jobId;

            int rows = startJobCmd.ExecuteNonQuery();

            if ((int)rc.Value == 1)
            {
                Logger.Log("[msdb].[dbo].[sp_start_job] failed to run.", System.Diagnostics.EventLogEntryType.Error);
                return false;
            }

            return true;
        }
        #region Helper Functions

        private void FillJobName()
        {
            SqlCommand cmd = new SqlCommand("SELECT [job_id], [name] FROM [msdb].[dbo].[sysjobs] WHERE [job_id] = @job_id", Database.Connection);
            cmd.CommandType = CommandType.Text;

            SqlParameter pJobId = cmd.Parameters.Add("@job_id", SqlDbType.NVarChar, 255);
            pJobId.Direction = ParameterDirection.Input;
            pJobId.Value = _jobId;

            SqlDataReader reader = cmd.ExecuteReader();

            //this._jobName = "No Name Found";

            if (reader.HasRows)
                this._jobName = reader.GetString(1);

            reader.Close();

        }
        private SqlAgentJobStatus GetJobStatus()
        {
            SqlCommand helpJobCmd = new SqlCommand("[msdb].[dbo].[sp_help_job]", Database.Connection);
            helpJobCmd.CommandType = CommandType.StoredProcedure;

            SqlParameter rc = helpJobCmd.Parameters.Add("RetVal", SqlDbType.Int);
            rc.Direction = ParameterDirection.ReturnValue;

            SqlParameter pJobId = helpJobCmd.Parameters.Add("@job_id", SqlDbType.NVarChar, 255);
            pJobId.Direction = ParameterDirection.Input;
            pJobId.Value = _jobId;

            SqlParameter pJobAspect = helpJobCmd.Parameters.Add("@job_aspect", SqlDbType.NVarChar, 255);
            pJobAspect.Direction = ParameterDirection.Input;
            pJobAspect.Value = "JOB";

            SqlDataReader reader = helpJobCmd.ExecuteReader();

            reader.Read();

            int jobStatus = (int)reader["current_execution_status"];

            reader.Close();

            switch (jobStatus)
            {
                case 0:
                    return SqlAgentJobStatus.NotIdleOrSuspended;
                case 1:
                    return SqlAgentJobStatus.Executing;
                case 2:
                    return SqlAgentJobStatus.WaitingForThread;
                case 3:
                    return SqlAgentJobStatus.BetweenRetries;
                case 4:
                    return SqlAgentJobStatus.Idle;
                case 5:
                    return SqlAgentJobStatus.Suspended;
                case 7:
                    return SqlAgentJobStatus.PerformingCompletitionActions;
                default:
                    return SqlAgentJobStatus.Unknown;
            }
        }
        #endregion

        public override string ToString()
        {
            return "SQLAgentJob(\"" + this._jobName + "\" - \"" + this._jobId + "\")";
        }
    }
}
