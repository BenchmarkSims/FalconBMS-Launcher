using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace FalconBMS.Launcher
{
    internal static class Utils
    {
        public static readonly UTF8Encoding UTF8_NO_BOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static StreamWriter CreateUtf8TextWihoutBom(string pathname, bool append=false)
        {
            UTF8Encoding utf8enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            return new StreamWriter(pathname, append, utf8enc);
        }

        public static Process LaunchProcess(string exe, string args=null, string cwd=null)
        {
            Diagnostics.Log($"Launching EXE: {exe} {args}", Diagnostics.LogLevels.Info);
            ProcessStartInfo psi = new ProcessStartInfo(exe, args);
            psi.UseShellExecute = true; //NB: will prompt UAC dialog for EXEs with manifest: requestedExecutionLevel=requireAdministrator
            psi.WorkingDirectory = cwd;

            return Process.Start(psi);
        }

        public static Process LaunchAppOrBrowserUrl(string url)
        {
            Diagnostics.Log($"Launching URL: {url}", Diagnostics.LogLevels.Info);
            ProcessStartInfo psi = new ProcessStartInfo(url);
            psi.UseShellExecute = true;

            return Process.Start(psi);
        }
    }
}
