-- [MailQueueId], [Date], [TemplateId], [MailJson], [Personalizations], [Batch], [LastAttempt], [Attempts]
CREATE TABLE [dbo].[SendGridForEpiMailQueueArchive]
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
	CONSTRAINT [PK_SendGridForEpiMailQueueArchive] PRIMARY KEY CLUSTERED 
(
	[MailQueueId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO