namespace Krompaco.SendGridForEpi.SqlServer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SqlServerService
    {
        public SqlConnection GetNewConnection()
        {
            var config = new SendGridForEpi.SqlServer.Configuration();
            return new SqlConnection(ConfigurationManager.ConnectionStrings[config.SqlServerConnectionStringName].ConnectionString);
        }

        public void CreateTablesIfNeeded()
        {
            const string Script = @"IF NOT EXISTS(SELECT * FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[sendgridforepi_MailQueue]') AND type in (N'U'))
BEGIN

CREATE TABLE [dbo].[sendgridforepi_MailQueue]
(
	[MailQueueId] [bigint] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL,
	[TemplateId] [nvarchar](72) NOT NULL,
	[MailJson] [nvarchar](max) NOT NULL,
	[Personalizations] [int] NOT NULL,
	[Batch] [int] NOT NULL,
	[LastAttempt] [datetime] NOT NULL,
	[Attempts] [int] NOT NULL,
	[AttemptMessage] [nvarchar](max) NULL,
	CONSTRAINT [PK_sendgridforepi_MailQueue] PRIMARY KEY CLUSTERED
(
	[MailQueueId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END

IF NOT EXISTS(SELECT * FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[sendgridforepi_MailQueueArchive]') AND type in (N'U'))
BEGIN

CREATE TABLE [dbo].[sendgridforepi_MailQueueArchive]
(
	[MailQueueId] [bigint] NOT NULL,
	[Date] [datetime] NOT NULL,
	[TemplateId] [nvarchar](72) NOT NULL,
	[MailJson] [nvarchar](max) NOT NULL,
	[Personalizations] [int] NOT NULL,
	[Batch] [int] NOT NULL,
	[LastAttempt] [datetime] NOT NULL,
	[Attempts] [int] NOT NULL,
	[AttemptMessage] [nvarchar](max) NULL,
	CONSTRAINT [PK_sendgridforepi_MailQueueArchive] PRIMARY KEY CLUSTERED 
(
	[MailQueueId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END";

            using (SqlConnection connection = this.GetNewConnection())
            {
                connection.Open();

                var cmd = new SqlCommand(Script, connection);

                cmd.ExecuteNonQuery();
            }
        }
    }
}
