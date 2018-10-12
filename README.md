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

**app.config** for Escc.AzureEmailForwarder

	<configuration>
	  <configSections>
	    <section name="exceptionless" type="Exceptionless.Configuration.ExceptionlessSection, Exceptionless" />
	    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net"/>
	  </configSections>

	  <connectionStrings>
	    <add name="Escc.Services.Azure.EmailQueue" connectionString="UseDevelopmentStorage=true"/>
	  </connectionStrings>
	
	 <system.net>
	    <mailSettings>
	      <smtp>
	        <network host="IP of SMTP server" />
	      </smtp>
	    </mailSettings>
	  </system.net>

	  <log4net>
	     <appender name="rollingFile" type="log4net.Appender.RollingFileAppender">
		     <file type="log4net.Util.PatternString" value="Logs\log.txt" />
		     <lockingModel type="log4net.Appender.FileAppender+MinimalLock" />
		     <appendToFile value="true" />
		     <rollingStyle value="Date" />
		     <maximumFileSize value="5MB" />
		     <layout type="log4net.Layout.PatternLayout">
		        <conversionPattern value="%date %-5level %logger - %message%newline" />
		     </layout>
		     <encoding value="utf-8" />
		 </appender>
		 <root>
		     <appender-ref ref="rollingFile"/>
		 </root>
	  </log4net>
	
	  <exceptionless apiKey="project API key" serverUrl="Exceptionless server URL" />	
	</configuration>