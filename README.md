Escc.AzureEmailForwarder
========================

Windows Azure does not have a built-in SMTP service. One option is to serialise emails to Azure storage, and have an on-premise application watch the queue and send the emails using your existing SMTP server as [described on the Microsoft Azure blog](http://azure.microsoft.com/blog/2010/10/08/adoption-program-insights-sending-emails-from-windows-azure-part-1-of-2/).

The queuing code is adapted from the [Azure Friday series on queuing](http://channel9.msdn.com/Shows/Azure-Friday/Azure-Queues-101-Basics-of-Queues-with-Mark-Simms) (4 episodes) by Scott Hanselman and Mark Simms. 

When you send an email using `AzureQueuedEmailSender` it serialises the email to a blob (to avoid any size limit for attachments), then stores the URL of the blob in a queue. This application runs on-premise with access to your own SMTP server. It watches the queue and either forwards emails and deletes them from the storage account or, if it fails for some reason, it moves the URL to table storage for manual processing, so that it does not keep reappearing on the queue or eventually expire from it.

We use [Exe2Srv](http://www.codeproject.com/Articles/715967/Running-Redis-as-a-Windows-Service) to run this application as a Windows Service. Exe2Srv and example configuration files are included in the Exe2Srv folder.

We use [Log4Net](http://logging.apache.org/log4net/) and [Exceptionless](http://exceptionless.com/) for logging and error reporting, but you can substitute any `ILogger` implementation.

**C#** 

 In this example configuration settings come from web.config or app.config, but they could come from any `IServiceRegistry`. The ASP.NET application cache is being used to avoid recreating the email service, but this could be any `IServiceCacheStrategy` or none.

	var email = new MailMessage("alice@example.org", "bob@example.org", "subject", "body");
	var configuration = new ConfigurationServiceRegistry();
	var cache = new HttpContextCacheStrategy();

	var emailService = ServiceContainer<IEmailSender>.LoadService(configuration, cache);

    emailService.Send(email); 

**web.config** for sending email

	<configuration>
	  <configSections>
	    <sectionGroup name="Escc.Services">
	      <section name="ServiceRegistry" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
	    </sectionGroup>
	  </configSections>
	  
	  <Escc.Services>
	    <ServiceRegistry>
	      <add key="Escc.Services.IEmailSender" value="Escc.Services.Azure.AzureQueuedEmailSender, Escc.Services.Azure" />
	    </ServiceRegistry>
	  </Escc.Services>
	
	  <connectionStrings>
	    <add name="Escc.Services.Azure.EmailQueue" connectionString="UseDevelopmentStorage=true"/>
	  </connectionStrings>
	</configuration>

For **app.config** for Escc.AzureEmailForwarder, see [app.example.config](Escc.AzureEmailForwarder\App.example.config).

## Escc.AzureEmailForwarder.Monitor

This .NET Core web application lets you monitor the blobs in the storage account. This is useful when there is a problem and emails fail to send. You can view and send the failed emails, or delete the blob if the email is no longer relevant.

The application is a first draft and lacks user security, so it should only deployed if security is configured at the web server level. It also lacks management of the `badmail` table, looking instead directly at blob storage. This means that:

* you need manually to check the date of the email as, if it's very recent, it may still be about to be processed automatically
* if you click 'send' or 'delete', the blob is deleted but the corresponding record in the `badmail` table needs to be deleted manually 