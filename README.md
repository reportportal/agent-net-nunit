[![Build status](https://ci.appveyor.com/api/projects/status/q4l1kw3xrbi79m7i/branch/master?svg=true)](https://ci.appveyor.com/project/nvborisenko/agent-net-nunit/branch/master)

# Installation
Install **ReportPortal.NUnit 3+** NuGet package into your project with tests.

[![NuGet version](https://badge.fury.io/nu/reportportal.nunit.svg)](https://badge.fury.io/nu/reportportal.nunit)
> PS> Install-Package ReportPortal.NUnit -Version 3.0.0-beta-21 -Pre

To enable NUnit extension you have to add `ReportPortal.addins` file in the folder where NUnit Runner is located. The content of the file should contain line with path to the `ReportPortal.NUnitExtension.dll`. To read more about how NUnit is locating extensions please follow [this](https://github.com/nunit/docs/wiki/Engine-Extensibility#locating-addins).

Imagine you have the next folders structure:

```
C:
|- NUnitRunner
  |- nunit.console.exe
  |- ReportPortal.addins
|- YourProject
  |- bin
    |- Debug
      |- YourProject.Tests.dll
      |- ReportPortal.NUnitExtension.dll
```

To enable ReportPortal.Extension you need create a `ReportPortal.addins` file in the `NUnitRunner` folder with the following content:
```
../YourProject/bin/Debug/ReportPortal.NUnitExtension.dll
```


# Configuration
NuGet package installation adds `ReportPortal.conf` file with configuration of the integration.

Example of config file:
```json
{
  "enabled": true,
  "server": {
    "url": "https://rp.epam.com/api/v1/",
    "project": "default_project",
    "authentication": {
      "uuid": "aa19555c-c9ce-42eb-bb11-87757225d535"
    },
    /* "proxy": "http://host:port" */
  },
  "launch": {
    "name": "NUnit 3 Demo Launch",
    "description": "this is description",
    "debugMode": true,
    "tags": [ "t1", "t2" ]
  }
}
```
Proxy element is optional.


# Logging integration
By default nunit extension sends console output to Report Portal at the end of test. To see log messages in realtime please follow [Logging Integration](http://reportportal.io/#documentation/Logging-Integration) instructions.

After that just add class somewhere into your project:
```csharp
    public class BridgeExtension : ReportPortal.Shared.IBridgeExtension
    {
        public void Log(ReportPortal.Client.Models.LogLevel level, string message)
        {
            NUnit.Framework.TestContext.Progress.WriteLine(message);
        }
    }
```


# Example
Follow [reportportal example-net-nunit](https://github.com/reportportal/example-net-nunit) repo to see the source of test project with Report Portal integration.


# Video tutorial
Integration tutorial by @Kate.yurasova

<a href="http://www.youtube.com/watch?feature=player_embedded&v=BsU-DjBx-DQ
" target="_blank"><img src="http://img.youtube.com/vi/BsU-DjBx-DQ/0.jpg" 
alt="Report Portal - Setup Integration with Nunit" width="240" height="180" border="10" /></a>
