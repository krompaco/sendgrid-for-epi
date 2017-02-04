namespace Krompaco.SendGridForEpi.SqlServer
{
    using System.Configuration;

    public class Configuration
    {
        public Configuration()
        {
            this.SqlServerConnectionStringName = ConfigurationManager.AppSettings["sendgridforepi:SqlServerConnectionStringName"];

            if (string.IsNullOrWhiteSpace(this.SqlServerConnectionStringName))
            {
                this.SqlServerConnectionStringName = "EPiServerDB";
            }
        }

        public string SqlServerConnectionStringName { get; set; }
    }
}
