using FreeSql;

namespace SerialConverter.Plugins.VirtualSerial.DBHelper
{
    public class DB
    {
        private static readonly string? AppPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

        public static Lazy<IFreeSql> sqliteLazy = new Lazy<IFreeSql>(() =>
            {
                IFreeSql fsql = new FreeSqlBuilder()
                    .UseAdoConnectionPool(true)
                    .UseConnectionString(DataType.Sqlite, @$"Data Source={AppPath}/VritualPort.db")
                    .UseAutoSyncStructure(true)
                    .Build();
                return fsql;
            });
        public static IFreeSql DataBase => sqliteLazy.Value;
        ~DB()
        {
            if (sqliteLazy.IsValueCreated)
            {
                sqliteLazy.Value.Dispose();
            }
        }
    }
}
