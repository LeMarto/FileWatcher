using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWatcherBackend
{
    public class FileWatcher
    {
        public delegate void FileReadyForProcessing(FileWatcher watcher);
        public event FileReadyForProcessing OnFileReadyForProcessing;
        public event FileReadyForProcessing OnFileRemovedFromFolder;

        public const NotifyFilters NOTIFY_FILTERS = NotifyFilters.LastWrite | NotifyFilters.FileName;

        private string _path;
        private string _fileName;
        private string _jobId;

        public readonly SqlAgentJob JobHandler;

        private readonly FileSystemWatcher _watcher;
        public bool Enabled
        {
            get { return _watcher.EnableRaisingEvents; }
            set
            {
                //As this was disabled. If the file exists then raise the event?
                if(_watcher.EnableRaisingEvents == false && value == true && FileExists() && JobHandler.Status == SqlAgentJobStatus.Idle)
                {
                    Logger.Log("Found file \"" + _fileName + "\" at \"" + _path + "\" when enabled. Maybe we missed it? just in case adding it to the queue.");
                    if (OnFileReadyForProcessing == null)
                        return;

                    OnFileReadyForProcessing(this);
                }
                _watcher.EnableRaisingEvents = value;
            }
        }
        public FileWatcher(string path, string fileName, string jobId)
        {
            _path = path;
            _fileName = fileName;
            _jobId = jobId;

            JobHandler = Database.CreateSqlAgentJobHandler(_jobId);

            _watcher = new FileSystemWatcher();
            _watcher.Path = _path;
            _watcher.Filter = _fileName;

            _watcher.NotifyFilter = NOTIFY_FILTERS;

            _watcher.Changed += new FileSystemEventHandler(OnFileChanged);
            _watcher.Created += new FileSystemEventHandler(OnFileChanged);
            _watcher.Deleted += new FileSystemEventHandler(OnFileRemoved);
            _watcher.Renamed += new RenamedEventHandler(OnFileRenamed);

            //Do not start listening inmediatly.
            _watcher.EnableRaisingEvents = false;
        }
        #region Event Handlers
        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (OnFileReadyForProcessing == null)
                return;

            OnFileReadyForProcessing(this);
        }
        private void OnFileRenamed(object source, RenamedEventArgs e)
        {
            if (OnFileReadyForProcessing == null)
                return;

            OnFileReadyForProcessing(this);
        }
        private void OnFileRemoved(object source, FileSystemEventArgs e)
        {
            if (OnFileRemovedFromFolder == null)
                return;

            OnFileRemovedFromFolder(this);
        }
        #endregion
        #region Helper Functions
        public bool FileExists()
        {
            return File.Exists(_path + "\\" + _fileName);
        }
        public bool FileLocked()
        {

            FileInfo fileInfo = new FileInfo(_path + "\\" + _fileName);
            FileStream fileStream = null;

            try
            {
                fileStream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception e)
            {
                Logger.Log("Exception: " + e.Message, System.Diagnostics.EventLogEntryType.Warning);
                return true;
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
            }
            //file is not locked
            return false;
        }
        public override string ToString()
        {
            return "FileWatcher(\"" + this._path + "\\" + this._fileName + "\") -> " + this.JobHandler;
        }
        #endregion
    }
}
