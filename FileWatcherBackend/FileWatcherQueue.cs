using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FileWatcherBackend
{
    public class FileWatcherQueue
    {
        public const int TIMER_INTERVAL = 60000; // 60 seconds  
        private List<FileWatcher> _watchers;
        private Queue<FileWatcher> _watchersReady;
        private Timer _timer;
        public FileWatcherQueue(string pathToFile)
        {
            _watchers = new List<FileWatcher>();
            _watchersReady = new Queue<FileWatcher>();
            LoadFromFile(pathToFile);
            _timer = new Timer();
            _timer.Interval = TIMER_INTERVAL; // 60 seconds  
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimer);
            _timer.Stop();
        }
        private void LoadFromFile(string pathToFile)
        {
            string json = File.ReadAllText(pathToFile);
            JsonTextReader reader = new JsonTextReader(new StringReader(json));
            bool rv;

            //file must start with an array opener
            rv = reader.Read();
            while (rv && reader.TokenType != JsonToken.StartArray)
            {
                rv = reader.Read();
            }

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                // object detected
                if (reader.TokenType == JsonToken.StartObject)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    //read stuff until we detect the end of the object
                    while (reader.TokenType != JsonToken.EndObject)
                    {
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            string propertyName = reader.Value.ToString();
                            //this means that the next read should have a property value
                            rv = reader.Read();

                            if (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Boolean || reader.TokenType == JsonToken.Date)
                                properties[propertyName] = reader.Value.ToString();
                        }
                        rv = reader.Read();
                    }
                    
                    //test if the jobid exists on the database
                    SqlAgentJob jobHandler = Database.CreateSqlAgentJobHandler(properties["jobid"]);
                    if (jobHandler == null)
                        Logger.Log("Job Id \"" + properties["jobid"] + "\" from file \"" + properties["name"] + "\" at folder \"" + properties["folder"] + "\" was not found on server " + Database.Datasource + ". Job was ignored.", System.Diagnostics.EventLogEntryType.Warning);


                    FileWatcher watcher = new FileWatcher(properties["folder"], properties["name"], properties["jobid"]);
                    watcher.OnFileReadyForProcessing += OnFileReadyForProcessing;
                    watcher.OnFileRemovedFromFolder += OnFileRemoved;
                    _watchers.Add(watcher);
                    
                }
            }
            Logger.Log("files.json load complete. Listening to " + _watchers.Count.ToString() + " files.");
        }
        public void StartListening()
        {
            _timer.Start();
            foreach (FileWatcher watcher in _watchers)
            {
                watcher.Enabled = true;
            }
        }
        public void StopListening()
        {
            _timer.Stop();
            foreach (FileWatcher watcher in _watchers)
            {
                watcher.Enabled = false;
            }
        }
        private void OnFileReadyForProcessing(FileWatcher watcher)
        {
            //do not add an already enqueue job. This event can be called multiple times per file triggering.
            if (_watchersReady.Contains(watcher))
                return;

            _watchersReady.Enqueue(watcher);
            Logger.Log(watcher + " queued.");
        }

        private void OnFileRemoved(FileWatcher watcher)
        {
            if (_watchersReady.Count == 0)
                return;

            //do not add an already enqueue job. This event can be called multiple times per file triggering.
            if (!_watchersReady.Contains(watcher))
                return;

            for (int i = 0; i < _watchersReady.Count; i++)
            {
                FileWatcher current = _watchersReady.Dequeue();
                if (current != watcher)
                    _watchersReady.Enqueue(current);
            }

            Logger.Log("Removed " + watcher + " from queue as it was deleted in the folder.");
        }
        private void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {

            if (_watchersReady.Count == 0)
                return;

            FileWatcher watcher = _watchersReady.Dequeue();

            if (!watcher.FileExists())
            {
                OnFileRemoved(watcher);
                return;
            }

            if (watcher.FileLocked())
            {
                Logger.Log("File for " + watcher + " is locked. Pushing to the bottom of the queue...", System.Diagnostics.EventLogEntryType.Warning);
                _watchersReady.Enqueue(watcher);
                return;
            }

            bool result = watcher.JobHandler.Run();

            if (result)
                Logger.Log("Triggering " + watcher.JobHandler + "\"...");
            else
                Logger.Log("Triggering of " + watcher.JobHandler + " failed.  Current job status = " + watcher.JobHandler.Status.ToString());
        }
    }
}
