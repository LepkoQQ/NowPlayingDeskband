using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace NowPlayingDeskband
{
    class SimpleLogger
    {
        private static readonly SimpleLogger DefaultLogger = new SimpleLogger("default.log");

        private readonly object LockObject = new object();
        private readonly string FilePath;

        public SimpleLogger(string name) {
            var AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var FolderPath = Path.Combine(AppDataPath, "NowPlayingDeskband");
            Directory.CreateDirectory(FolderPath);
            FilePath = Path.Combine(FolderPath, name);
        }

        public void Log(string line) {
            var timeStamp = DateTime.Now.ToString("s");
            var threadId = Thread.CurrentThread.ManagedThreadId;

            var fullLine = $"[{timeStamp}][Thread {threadId}] {line}";

            WriteLineToDebug(fullLine);
            WriteLineToFile(fullLine);
        }

        [Conditional("DEBUG")]
        private void WriteLineToDebug(string line) {
            Debug.WriteLine(line);
        }

        [Conditional("RELEASE")]
        private void WriteLineToFile(string line) {
            lock (LockObject) {
                File.AppendAllText(FilePath, line + "\n");
            }
        }

        public static void DefaultLog(string line) {
            DefaultLogger.Log(line);
        }
    }
}
