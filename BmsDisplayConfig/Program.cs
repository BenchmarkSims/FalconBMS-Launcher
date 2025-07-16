using System;
using System.Reflection;
using System.Windows;

namespace BmsDisplayConfig
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                Assembly thisExe = Assembly.GetExecutingAssembly();
                Logger.WriteLine("Program.Main, version "+ thisExe.GetName().Version);

#if DEBUG
                string info_ver = thisExe.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                Logger.WriteLine(info_ver);
#endif

                var app = new App();
                app.InitializeComponent();
                return app.Run();
            }
            catch (Exception ex)
            {
                Logger.WriteLine("EXCEPTION:");
                Logger.WriteLine(ex.ToString());

                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg = ex.InnerException.Message;

                MessageBox.Show(msg, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
            }
            finally
            {
                Logger.Shutdown();
            }
            return 1;
        }
    }
}
