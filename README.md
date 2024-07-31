# DabdoubsElkAppender
A C# log4net appender to enable logging to elk

To install, download project file and make sure it builds correctly.
once installed, add as a project refrence and modify your log4net appender 
This setup guide assumes you've already got Elasticsearch installed on your local computer or network.

log4net has a lot of configuration options and you should read the full documentation at the log4net site. Below will cover what you need to get started using log4net.ElasticSearch today.

Get Started
Start by creating a .NET project and open the app.config file. Modify it like the example below. (For ASP.NET projects, modify the web.config instead) Be sure to add the new <configSection> first, just below the opening <configuration> tag.

<?xml version="1.0"?>
<configuration>
    <configSections>
        <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
    </configSections>
    <startup>
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.0"/>
    </startup>
    <log4net>
        <appender name="ElasticSearchAppender" type="ElkTestNetFramework.ElasticSearchAppender, ElkTestNetFramework">
            <connectionString value="Scheme=https;User=username;Pwd=password;Server=localhost;Index=log;Port=9200;rolling=true"/>
            <lossy value="false" />
            <evaluator type="log4net.Core.LevelEvaluator">
                    <threshold value="ERROR" />
            </evaluator>
            <bufferSize value="100" />
        </appender>
        <root>
            <level value="ALL"/>
            <appender-ref ref="ElasticSearchAppender" />
        </root>
    </log4net>
</configuration>
Alternatively you can create your own configuration.xml file and then reference it directly in the AssemblyInfo.cs file (see below)

Remember to set the properties of an external configuration.xml file so that it will be copied to the bin folder of your project. It must be present with the other assemblies.

Settings
Modify the <connectionString> node to match the server location/IP of your Elasticsearch server. If you're using Elasticsearch Shield, add the user and password settings respectively. If you'd like to have new indexes created daily, add the rolling=true field.

For a full explanation of the appender settings, see the next section of documentation.

Modify AssemblyInfo.cs
Open the AssemblyInfo.cs file under the project properties and add this line at the end:

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
or if you've got an external config file

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "configuration.xml", Watch = true)]`
For ASP.NET Projects
For ASP.NET projects, in either ApplicationStart() or other startup task class method, add:

log4net.Config.XmlConfigurator.Configure();`
Install log4net.ElasticSearch
Next we'll need to install the log4net.ElasticSearch Nuget package. Open your package manager console or the Nuget UI and install the package. Installing log4net.ElasticSearch will automatically install log4net as a dependency.

PS> Install-Package log4net.ElasticSearch`
Get Logging
Now that you've got log4net.ElasticSearch installed, you should be able to log your first message. Modify the Program.cs file accordingly:

using log4net;

namespace ConsoleApplication1
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            _log.Error("kaboom!", new ApplicationException("shit went down and it wasn't good"));
        }
    }
}
It is not strictly required to setup an index on Elasticsearch ahead of time, one will be created automatically when you log messages to the server. Index and template creation are covered in the Elasticsearch documentation.
here is an example log
{
	"_index": "log-2016.02.12",
	"_type": "logEvent",
	"_id": "AVLXHEwEJfnUYPcgkJ5r",
	"_version": 1,
	"_score": 1,
	"_source": {
		"timeStamp": "2016-02-12T20:11:41.5864254Z",
		"message": "Something broke.",
		"messageObject": {},
		"exception": {
			"Type": "System.Exception",
			"Message": "There was a system error",
			"HelpLink": null,
			"Source": null,
			"HResult": -2146233088,
			"StackTrace": null,
			"Data": {
				"CustomProperty": "CustomPropertyValue",
				"SystemUserID": "User43"
			},
			"InnerException": null
		},
		"loggerName": "log4net.ES.Example.Program",
		"domain": "log4net.ES.Example.vshost.exe",
		"identity": "",
		"level": "ERROR",
		"className": "log4net.ES.Example.Program",
		"fileName": "C:\\Users\\jtoto\\projects\\log4net.ES.Example\\log4net.ES.Example\\Program.cs",
		"lineNumber": "26",
		"fullInfo": "log4net.ES.Example.Program.Main(C:\\Users\\jtoto\\projects\\log4net.ES.Example\\log4net.ES.Example\\Program.cs:26)",
		"methodName": "Main",
		"fix": "LocationInfo, UserName, Identity, Partial",
		"properties": {
			"log4net:Identity": "",
			"log4net:UserName": "JToto",
			"log4net:HostName": "JToto01",
			"@timestamp": "2016-02-12T20:11:41.5864254Z"
		},
		"userName": "JToto",
		"threadName": "9",
		"hostName": "JTOTO01"
	}
}
