using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BmsDisplayConfig
{
    //--------------------------------------------------------------
    internal class Logger
    {
        static StreamWriter s_logfile;

        static Logger()
        {
            string logpath = Path.Combine(System.IO.Path.GetTempPath(), "BmsDisplayConfig.log");
            s_logfile = File.CreateText(logpath);
        }

        public static void Shutdown()
        {
            s_logfile.Flush();
            s_logfile.Close();
            s_logfile = null;
        }

        public static void WriteLine(string line)
        {
            s_logfile.WriteLine(line);
            s_logfile.Flush();

            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debug.WriteLine(line);
        }
    }
}