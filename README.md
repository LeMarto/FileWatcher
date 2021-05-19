# File-Watcher
A C# MS Windows service that monitors files and triggers sql agent jobs when the contents of said files change.

## Projects

### FileWatcherBackend
A Library with the all the logic so that its easier to test without installing the service.

### FileWatcherConsole
Just a console application used to test the logic in FileWatcherBackend.

### FileWatcherService
The actual MS Windows Service.
Make sure to configure the properties in the settings:
* datasource: The actual server where the jobs will be triggered.

## Install Instructions

After a full build of the solution, copy the following files to any folder you want:
* FileWatcherBackend.dll
* Newtonsoft.Json.dll
* FileWatcherService.exe
* FileWatcherService.exe.config

Then create a file called files.json in that same directory. The format should be like so:
```javascript
[
	{
		"name": "Global Scorecard.CSV",
		"folder": "E:\\ETL\\Compensation",
		"jobid": "23728C08-9DEF-452E-85EA-A774CD5C98EB"
	}
]
```
Where "name" is the name of the file to be listened to, "folder" the folder where the file shold be listened to, and "jobid" the job_id of the sql agent job that should be triggered once the file change has been detected.

Then, run the following command with administrator rights from a terminal:
> installUtil "FileWatcherService.exe"

And provide both user and password from the service account the service will use to run.

_(Make sure that the installUtil location is included in the PATH of the terminal)_

## Uninstall Instructions
Run the following command with administrator rights from a terminal:
> installUtil /u "FileWatcherService.exe"

_(Make sure that the installUtil location is included in the PATH of the terminal)_

## About the Service Account
The service account for the service must have rights to both run sql agent jobs as well as to query msdb.
*Make sure you provide the credentials correctly.*

## External Libraries / Packages
This service makes use of the Newtonsoft.Json library to handle json files.