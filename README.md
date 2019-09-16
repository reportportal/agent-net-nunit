[![Build status](https://ci.appveyor.com/api/projects/status/q4l1kw3xrbi79m7i/branch/master?svg=true)](https://ci.appveyor.com/project/nvborisenko/agent-net-nunit/branch/master)
[![NuGet Badge](https://buildstats.info/nuget/reportportal.nunit)](https://www.nuget.org/packages/reportportal.nunit)

# Installation
Install **ReportPortal.NUnit** NuGet package into your project with tests.

> PS> Install-Package ReportPortal.NUnit

> Note: Skip this section and start to configure the integration if you execute tests in Visual Studio Test Explorer (VS 15.9) with `NUnit3TestAdapter` package or via `dotnet test`. `NUnit3TestAdapter 3.14.0` and `ReportPortal.NUnit 3.7.0` starts to support `.net core 2`. Execute your netcore tests in Visual Studio Test Explorer, or via `dotnet test` or `dotnet vstest`.

To enable NUnit extension you have to add `ReportPortal.addins` file in the folder where NUnit Runner is located. The content of the file should contain line with relative path to the `ReportPortal.NUnitExtension.dll`. To read more about how NUnit is locating extensions please follow [this](https://github.com/nunit/docs/wiki/Engine-Extensibility#locating-addins).

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
Add `ReportPortal.config.json` file with configuration of the integration to test project. This file will be copied automatically to output folder during project building.

Example of config file:
```json
{
  "$schema": "https://raw.githubusercontent.com/reportportal/agent-net-nunit/master/src/ReportPortal.NUnitExtension/ReportPortal.config.schema",
  "enabled": true,
  "server": {
    "url": "https://rp.epam.com/api/v1/",
    "project": "default_project",
    "authentication": {
      "uuid": "aa19555c-c9ce-42eb-bb11-87757225d535"
    }
  },
  "launch": {
    "name": "NUnit 3 Demo Launch",
    "description": "this is description",
    "debugMode": false,
    "tags": [ "t1", "t2" ]
  }
}
```

# Environment variables
It's possible to override parameters via environment variables.
```cmd
set reportportal_launch_name="My new launch name"
# execute tests
```

`reportportal_` prefix is used for naming variables, and `_` is used as delimeter. For example to override `Server.Authentication.Uuid` parameter, we need specify `ReportPortal_Server_Authentication_Uuid` in environment variables. To override launch tags we need specify `ReportPortal_Launch_Tags` with `tag1;tag2` value (`;` used as separator for list of values).

# Customization

You can customize a test run in order to have a user-friendly report. Following customization is supported:
* update run/feature/test name
* update run/feature/test description
* add run/feature/test tags

Please note, test categories are added to tags and test description is added to description by default

Add a class that implements `NUnit.Engine.ITestEventListener` (from NUnit.Engine.Api package) to a project. Assume the class is implemented within the YourProject.Tests project. To enable your extension you need to add path to the project assembly to `ReportPortal.addins` file in the `NUnitRunner` folder with the following content (see folder structure above):  

```
../YourProject/bin/Debug/YourProject.Tests.dll
```

Twelve handlers are available for event subscription that can be represented with following combination: [Before/After][Run/Suite/Test][Started/Finished]. The subscription is implemented in the constructor.

See deatils of the customization in the [example](https://github.com/reportportal/example-net-nunit/blob/master/src/Example/ReportPortalCustomization/Customization.cs)

# Example
Follow [reportportal example-net-nunit](https://github.com/reportportal/example-net-nunit) repo to see the source of test project with Report Portal integration.

# Integrate logger framework
- [NLog](https://github.com/reportportal/logger-net-nlog)
- [log4net](https://github.com/reportportal/logger-net-log4net)
- [Serilog](https://github.com/reportportal/logger-net-serilog)
- [System.Diagnostics.TraceListener](https://github.com/reportportal/logger-net-tracelistener)

# Useful extensions
- [SourceBack](https://github.com/nvborisenko/reportportal-extensions-sourceback)
