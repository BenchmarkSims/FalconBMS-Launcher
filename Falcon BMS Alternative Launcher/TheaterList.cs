using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using FalconBMS.Launcher.Input;

namespace FalconBMS.Launcher
{
    public class TheaterList
    {
        /// <summary>
        /// Read theater.lst and apply the list to Combobox.
        /// </summary>
        public static void PopulateAndSave(AppRegInfo appReg, ComboBox comboBox)
        {
            if (!Directory.Exists(appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER))
                Directory.CreateDirectory(appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER);

            string filename = appReg.GetInstallDir() + "/Data/Terrdata/TheaterDefinition/theater.lst";
            string fbackupname = appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + "theater.lst";
            if (!File.Exists(fbackupname) && File.Exists(filename))
                File.Copy(filename, fbackupname, false);
            File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            // Recursively scan all subdirectories, for *.tdf files.
            string dataRoot = Path.Combine(appReg.GetInstallDir(), "Data");
            var theaterFiles = Directory.GetFiles(dataRoot, "*.tdf", SearchOption.AllDirectories);

            // KoreaKTO should be at the Top or MC will have a problem. I'd say MC should fix this!
            Array.Sort(theaterFiles, 
                (a, b) => {
                    if (a.EndsWith("\\Korea KTO.tdf")) return -1;
                    if (b.EndsWith("\\Korea KTO.tdf")) return +1;
                    return String.CompareOrdinal(a, b);
                    }
                );

            // Write relative paths for all TDFs to the theater list (relative to the /Data subdir).
            File.WriteAllLines(filename, theaterFiles.Select(t => t.Substring(dataRoot.Length).TrimStart('\\')));

            // Populate the combobox.
            List<string> theaters = new List<string>();
            foreach (string tdf in theaterFiles)
            {
                //Hack: ignore tdf output from F4Patch, used by some theater configs
                if (tdf.Contains("F4Patch"))
                    continue;

                IEnumerable<string> lines = File.ReadLines(tdf, Encoding.UTF8);
                foreach (string str in lines)
                {
                    if (str.StartsWith("name "))
                    {
                        theaters.Add(str.Replace("name ", "").Trim());
                        break;
                    }
                }
            }

            comboBox.SelectedIndex = -1;
            comboBox.Items.Clear();

            foreach (string t in theaters)
            {
                comboBox.Items.Add(t);
                if (t == appReg.GetCurrentTheater())
                    comboBox.SelectedItem = t;
            }
        }
    }
}
