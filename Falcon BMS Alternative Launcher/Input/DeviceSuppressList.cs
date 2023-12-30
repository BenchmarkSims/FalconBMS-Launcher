using System;
using System.IO;
using System.Collections.Generic;

namespace FalconBMS.Launcher.Input
{
    public class DeviceSuppressList
    {
        List<Guid> _blockedIds = new List<Guid>();

        public DeviceSuppressList()
        {
            // Load list of pidvid/instance GUIDs from config file.
            string configDir = Path.Combine(
                Program.mainWin.appReg.GetInstallDir(),
                "User\\Config");

            string suppressFile = Path.Combine(configDir,
                @"DeviceSuppress.txt");

            if (!File.Exists(suppressFile)) return;

            using (StreamReader sr = File.OpenText(suppressFile))
            {
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null) break;
                    if (String.IsNullOrEmpty(line)) continue;

                    string substr = line.Substring(0, 38);
                    if (substr.Length < 38) continue;

                    Guid guid;
                    if (Guid.TryParseExact(substr, format:"B", out guid))
                        this._blockedIds.Add(guid);
                }
            }
        }

        public bool IsDeviceSuppressed(Guid id)
        {
            return _blockedIds.Contains(id);
        }

    }
}
