namespace Krompaco.SendGridForEpi.SqlServer;

public class Configuration
{
    public Configuration(string sqlServerConnectionString)
    {
        this.SqlServerConnectionString = sqlServerConnectionString;
    }

    public string SqlServerConnectionString { get; set; }
}
