using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Esp
{
    public class Logger
    {
        public static bool firstRun = true;
        public static string logPath = "";
        public static void Setup()
        {
            firstRun = false;
            logPath = $@"{Utils.LogPath}/Log_{DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss")}.txt";
            File.WriteAllText(logPath, "-----Log initiated for esp.-----\nRun by " + Environment.UserName + ". Local time: " + DateTime.Now + "; UTC: " + DateTime.UtcNow + ".\n---------------------------------------------");

        }
        public static void Log(string Message, LogLevel level = LogLevel.LOG)
        {
            if (firstRun)
                Setup();
            File.AppendAllText(logPath, $"\n[{level} {DateTime.Now.ToString("HH:mm:ss")}]{Message}");
        }
        public enum LogLevel
        {
            LOG,
            WRN,
            ERR,
            EXC
        }
    }
}