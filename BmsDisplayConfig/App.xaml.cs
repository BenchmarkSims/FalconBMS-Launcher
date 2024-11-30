using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                Logger.WriteLine("Program.Main");

                var app = new App();
                app.InitializeComponent();
                return app.Run();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg = ex.InnerException.Message;

#if DEBUG
                msg = ex.ToString();
#endif

                MessageBox.Show(msg, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
            }
            finally
            {
                Logger.Shutdown();
            }
            return 1;
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }
}
