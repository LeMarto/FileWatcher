using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileWatcherBackend;
using System.IO;
using System.Reflection;

namespace FileWatcherConsole
{
    class Program
    {
        private static DirectoryInfo GetExecutingDirectory()
        {
            /*
             * Based on https://www.red-gate.com/simple-talk/blogs/c-getting-the-directory-of-a-running-executable/
             */
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory;
        }
        static void Main(string[] args)
        {
            string path = Uri.UnescapeDataString(GetExecutingDirectory().FullName) + "\\files.json";
            Database.InitConnection("abids-test.avaya.com");
            FileWatcherQueue queue = new FileWatcherQueue(path);
            Database.Close();
        }
    }
}
