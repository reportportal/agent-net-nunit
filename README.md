[![Build status](https://ci.appveyor.com/api/projects/status/q4l1kw3xrbi79m7i/branch/master?svg=true)](https://ci.appveyor.com/project/nvborisenko/agent-net-nunit/branch/master)

# Installation
Install **ReportPortal.NUnit 3+** NuGet package into your project with tests.

[![NuGet version](https://badge.fury.io/nu/reportportal.nunit.svg)](https://badge.fury.io/nu/reportportal.nunit)
> PS> Install-Package ReportPortal.NUnit

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

To verify installed extension just execute:
```
nunit3-console.exe --list-extensions
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

# Send a screenshot

To log a screenshot to a test run you can use Bridge.LogMessage method. You need to encode an image as base64 string and pass it to the method in following format: "{rp#base64#(.*)#(.*)}". First parameter is MIME type, second parameter is the base64 string image representation. In the example below a screenshot is taken with Selenium Web driver instance and send to Report portal if a test fails. Do not forget to add needed references to your test project.

```csharp
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using ReportPortal.Shared;
using LogLevel = ReportPortal.Client.Models.LogLevel;

// ...

	[TearDown]
	public void NunitTearDown()
	{
		if (TestContext.CurrentContext.Result.Outcome.Equals(ResultState.Failure) ||
			TestContext.CurrentContext.Result.Outcome.Equals(ResultState.Error) ||
			TestContext.CurrentContext.Result.Outcome.Equals(ResultState.SetUpError) ||
			TestContext.CurrentContext.Result.Outcome.Equals(ResultState.SetUpFailure))
		{
			string base64 = ((ITakesScreenshot) WebDriver.Instance).GetScreenshot().AsBase64EncodedString; 

			if (base64 != null)
			{
				Bridge.LogMessage(LogLevel.Error, "Screen shot on failure {rp#base64#image/png#" + base64 + "}");
			}
		}
	}
```

# Customization

You can customize a test run in order to have a user-friendly report. Following customization is supported:
* update run/feature/test name
* update run/feature/test description
* add run/feature/test tags

Please note, test categories are added to tags and test description is added to description by default

Add a class that implements NUnit.Engine.ITestEventListener to a project. Assume the class is implemented within the YourProject.Tests project. To enable your extension you need to add path to the project assembly to `ReportPortal.addins` file in the `NUnitRunner` folder with the following content (see folder structure above):  

```
../YourProject/bin/Debug/YourProject.Tests.dll
```

Twelve handlers are available for event subscription that can be represented with following combination: [Before/After][Run/Suite/Test][Started/Finished]. The subscription is implemented in the constructor.

See deatils of the customization in the [example](https://github.com/reportportal/example-net-nunit/blob/master/src/Example/ReportPortalCustomization/Customization.cs)

# Example
Follow [reportportal example-net-nunit](https://github.com/reportportal/example-net-nunit) repo to see the source of test project with Report Portal integration.


# Video tutorial
Integration tutorial by @Kate.yurasova

<a href="http://www.youtube.com/watch?feature=player_embedded&v=BsU-DjBx-DQ
" target="_blank"><img src="http://img.youtube.com/vi/BsU-DjBx-DQ/0.jpg" 
alt="Report Portal - Setup Integration with Nunit" width="240" height="180" border="10" /></a>
