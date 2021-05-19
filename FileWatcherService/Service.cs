using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using FileWatcherBackend;

namespace FileWatcherService
{
    #region Service Status Informing Structures
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };
    #endregion
    public partial class FileWatcherService : ServiceBase
    {
        /*
         * based on https://docs.microsoft.com/en-us/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer#BK_CreateProject
         */
        private readonly string _servicePath;
        private FileWatcherQueue _queue;
        #region Helper Methods
        private static DirectoryInfo GetExecutingDirectory()
        {
            /*
             * Based on https://www.red-gate.com/simple-talk/blogs/c-getting-the-directory-of-a-running-executable/
             */
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory;
        }
        #endregion
        #region Service Status Informing methods
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
        #endregion
        public FileWatcherService()
        {
            InitializeComponent();
            _servicePath = Uri.UnescapeDataString(GetExecutingDirectory().FullName);
            Database.InitConnection(Properties.Settings.Default.datasource);
            _queue = new FileWatcherQueue(_servicePath + "\\files.json");
        }
        #region Service Events
        protected override void OnStart(string[] args)
        {
            #region Inform pending status
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion
            _queue.StartListening();
            #region Inform running status
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion
        }

        protected override void OnPause()
        {
            #region Inform pending status
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_PAUSE_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion
            _queue.StopListening();
            #region Inform paused status
            serviceStatus.dwCurrentState = ServiceState.SERVICE_PAUSED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion
        }
        protected override void OnContinue()
        {
            #region Inform pending status
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_CONTINUE_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion
            _queue.StartListening();
            #region Inform running status
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion
        }
        protected override void OnStop()
        {
            #region Inform pending status
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion
            _queue.StopListening();
            Database.Close();
            #region Inform stopped status
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion
        }
        protected override void OnShutdown()
        {
            _queue.StopListening();
            Database.Close();
        }
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return true;
        }
        #endregion
    }
}
