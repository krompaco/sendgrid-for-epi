# SendGridForEpi
NuGet distributed packages for Episerver that handles async API-posting by a "local" queue storage and a Scheduled Job securing successful posts.

## Installation
Packages should be up on Episerver's NuGet feed shortly. The package Krompaco.SendGridForEpi.SqlServer will install both packages needed. Minimum Episerver version is 10.3.2 and SendGrid package is 8.0.5.

## Configuration
The main package requires you to add an app setting for the API Key. The key needs Mail Send Access and you set this up in Settings => API Keys in SendGrid.

    <add key="sendgridforepi:ApiKey" value="Usually.SG.followed.by.quite.a.long.string" />
   
When using the Krompaco.SendGridForEpi.SqlServer package you have the option to use another connection string name than EPiServerDB. If not present or empty "EPiServerDB" will be used and two tables named SendGridForEpiMailQueue and SendGridForEpiMailQueueArchive will be created on startup if not already present.

    <add key="sendgridforepi:SqlServerConnectionStringName" value="MyOwnDB" />

## How it works
You add SendGrid.Helpers.Mail.Mail (from the offical package) objects to a queue that is then processed and posted to the SendGrid API by a scheduled job. If error occurs job will try again next execution and log the number of attempts and latest error message.

The implementation handles batching of more than 1000 personalizations internally so you don't need to think about that limit.

My recommended approach is to hold mail templates and as much content as possible in SendGrid and work with Replacements. Posting through the API with SendGrid edited templates will automatically use template images to track mails opened and also rewrite links in your content so that clicks are tracked.

In the following example I have put the Subject line and the Body in SendGrid's template editor and added a couple of replacement keys in the form of {whoIs} and {commentText}. SendGrid allows you have any syntax style but recommends staying away from whitespace within a key name.

    var mail = new Mail
    {
        From = new Email("noreply@krompaco.nu"),
        TemplateId = "cf6d9ac4-****-480b-a95a-77921d060261"
    };

    mail.AddPersonalization(
        new Personalization()
        {
            Tos = new List<Email>
                    {
                        new Email("somedude@krompaco.nu")
                    },
            Substitutions = new Dictionary<string, string>
                    {
                        { "{whoIs}", "Some Dude" },
                        { "{commentText}", "A <b>bold</b> comment" }
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