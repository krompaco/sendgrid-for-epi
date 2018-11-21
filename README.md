# SendGridForEpi
NuGet distributed packages for Episerver that handles async API-posting by a "local" queue storage and a Scheduled Job securing successful posts.

## Installation
Packages are in Episerver's NuGet feed. If not set up go to Visual Studio => NuGet Package Manager => Package Sources => Add http://nuget.episerver.com/feed/packages.svc/

    Install-Package Krompaco.SendGridForEpi.SqlServer

This package will install both packages needed. Minimum Episerver version is 11.10.6 and SendGrid package is 9.10.0.


## Configuration
The main package requires you to add an app setting for the API Key. The key needs Mail Send Access and you set this up in Settings => API Keys in SendGrid.

    <add key="sendgridforepi:ApiKey" value="Usually.SG.followed.by.quite.a.long.string" />
   
When using the Krompaco.SendGridForEpi.SqlServer package you have the option to use another connection string name than EPiServerDB. If not present or empty "EPiServerDB" will be used. Two tables named SendGridForEpiMailQueue and SendGridForEpiMailQueueArchive will be created on startup if not already present in the configured database.

    <add key="sendgridforepi:SqlServerConnectionStringName" value="MyOwnDB" />

## How it works
You add SendGridMessage (from the offical package) objects to a queue that is then processed and posted to the SendGrid API by a scheduled job. If error occurs job will try again next execution and log the number of attempts and latest error message.

The implementation handles batching of more than 1000 personalizations internally so you don't need to think about that limit.

My recommended approach is to hold mail templates and as much content as possible in SendGrid and work with Handlebars syntax. Posting through the API with SendGrid edited templates will automatically use template images to track mails opened and also rewrite links in your content so that clicks are tracked.

In the following example I have put the Subject line and the Body in SendGrid's template editor and use this model for the dynamic data.

    private class CommentTemplateData
    {
        [JsonProperty("whoIs")]
        public string WhoIs { get; set; }

        [JsonProperty("commentText")]
        public string CommentText { get; set; }
    }

In the SendGrid template I will use Handlebars to output the data above using {{whoIs}} and triple-stash for the HTML property {{{commentText}}}.

    var mail = new SendGridMessage
    {
        From = new EmailAddress("noreply@krompaco.nu"),
        TemplateId = "the-id-found-in-the-sendgrid-template-editor",
        Personalizations = new List<Personalization>()
    };
    
    mail.Personalizations.Add(
        new Personalization()
        {
            Tos = new List<EmailAddress>
                    {
                        new EmailAddress("notifications@krompaco.nu")
                    },
                    TemplateData = new CommentTemplateData
                    {
                        WhoIs = "Some Name",
                        CommentText = "<p>First line of comment.</p><p>Second line of comment.</p>",
                    }
    });
    
    this.mailService.AddToQueue(new MailQueueItem
    {
        Date = DateTime.UtcNow,
        Mail = mail
    });

Assuming this code was in a Controller you would have gotten hold of the IMailService implementation in the constructor.

    private readonly IMailService mailService;
    
    public ArticlePageController(IMailService mailService)
    {
        this.mailService = mailService;
    }