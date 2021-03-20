using System;
using System.IO;

namespace NowPlayingDeskband
{
    class SimpleLogger
    {
        private static readonly SimpleLogger defaultLogger = new SimpleLogger("NowPlayingDeskband.log");

        private readonly string filePath;

        public SimpleLogger(string name) {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            filePath = Path.Combine(path, name);
        }

        public void Log(string line) {
            var timeStamp = DateTime.Now.ToString("s");
            var fullLine = "[" + timeStamp + "] " + line;

            System.Diagnostics.Debug.WriteLine(fullLine);
            //File.AppendAllText(filePath, fullLine + "\n");
        }

        public static void DefaultLog(string line) {
            defaultLogger.Log(line);
        }
    }
}
