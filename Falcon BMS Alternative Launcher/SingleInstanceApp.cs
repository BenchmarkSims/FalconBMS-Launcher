using System.Threading;

namespace FalconBMS.Launcher
{
    public static class SingleInstanceApp
    {
        internal static Mutex s_appGuardMutex = null;
        internal static string s_appGuardMutexName = @"Local\FalconBMS_Alternative_Launcher{7eeb504c-6767-4e99-a50c-eefbad25cb2a}";

        public static bool IsAlreadyRunning()
        {
            bool isNew;
            s_appGuardMutex = new Mutex(initiallyOwned: true, s_appGuardMutexName, out isNew);
            return (!isNew);
        }
    }
}
