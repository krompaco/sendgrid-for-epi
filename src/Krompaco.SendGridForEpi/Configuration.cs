namespace Krompaco.SendGridForEpi
{
    using System.Configuration;

    public class Configuration
    {
        public Configuration()
        {
            this.ApiKey = ConfigurationManager.AppSettings["sendgridforepi:ApiKey"];
        }

        public string ApiKey { get; set; }
    }
}
