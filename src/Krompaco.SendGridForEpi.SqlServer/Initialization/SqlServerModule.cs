namespace Krompaco.SendGridForEpi.SqlServer.Initialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
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
