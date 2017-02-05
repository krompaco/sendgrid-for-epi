namespace Krompaco.SendGridForEpi.SqlServer.Initialization
{
    using EPiServer.Framework;
    using EPiServer.Framework.Initialization;
    using Services;

    [InitializableModule]
    public class SqlServerModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var service = new SqlServerService();
            service.CreateTablesIfNeeded();
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}
