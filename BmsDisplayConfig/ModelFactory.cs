namespace BmsDisplayConfig
{
    //----------------------------------------
    internal static class DataModelFactory
    {
        public static IDataModel CreateDataModel()
        {
            //return new DataModel_Test();
            return new DataModel();
        }
    }

    //----------------------------------------
    internal static class SystemModelFactory
    {
        public static ISystemModel CreateSystemModel()
        {
            //return new SystemModel_Test();
            return new SystemModel();
        }
    }
}
