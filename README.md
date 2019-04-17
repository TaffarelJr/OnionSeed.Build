# OnionSeed.Build

Contains a set of files intended to reduce boilerplate when setting up a new [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/) solution and its associated build/[CI](https://en.wikipedia.org/wiki/Continuous_integration) process.

## Status

| Work in Progress                                                                                                                                               |                                                                                                                                                      |
|----------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| [![GitHub pull requests](https://img.shields.io/github/issues-pr-raw/TaffarelJr/OnionSeed.Build.svg?logo=github)](https://github.com/TaffarelJr/OnionSeed.Build) | [![GitHub issues](https://img.shields.io/github/issues-raw/TaffarelJr/OnionSeed.Build.svg?logo=github)](https://github.com/TaffarelJr/OnionSeed.Build) |

## [MSBuild Project](https://docs.microsoft.com/en-us/aspnet/web-forms/overview/deployment/web-deployment-in-the-enterprise/understanding-the-project-file) Property Files

These files are designed to encapsulate some of the standard settings that should be included in all `.csproj` files:

* `Production.props` - Contains settings for code that is to be published for production use. It includes a reference to:
	* `Production.ruleset` - Identifies the [CodeAnalysis](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) rules that should be applied to or excluded from production code.
* `Test.props` - Contains settings for test harness projects. It includes a reference to:
	* `Test.ruleset` - Identifies the [CodeAnalysis](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) rules that should be applied to or excluded from test code.
* `Common.props` - Contains general settings for both production and test code. These properties are automatically imported into the other two `.props` files, so there's no need to do anything with it. It includes a reference to:
	* `StyleCop.json` - Identifies the [StyleCop](https://github.com/StyleCop/StyleCop) settings that should be applied to both production and test code.

You can import these properties into your `.csproj` file by doing something like this for production projects:
```
<Project Sdk="Microsoft.NET.Sdk">
	...
	<Import Project="..\..\build\Production.props" />
	...
</Project>
```

... and something like this for test projects:
```
<Project Sdk="Microsoft.NET.Sdk">
	...
	<Import Project="..\..\build\Test.props" />
	...
</Project>
```

## [Cake](https://cakebuild.net/) Task Definitions

These files encapsulate a standard .NET build/test/deploy process, and can be re-used for any .NET solution. They can be executed locally as well as on a build server, and should perform identically in each location. The idea behind this is to have the build process under source control, instead of wrapped up in various UI settings on a build server somee.

* `Constants.cake` - Contains some constant value definitions, including the names of the [environment variables](https://en.wikipedia.org/wiki/Environment_variable) that are required:
	* `APPVEYOR_BUILD_VERSION` - Contains the version number for the current build. _Defaults to `1.0.0` if this value is missing._
	* `GIT_USERNAME` - The username to be used to tag the source code at `origin` with the current build number. For production builds only. _Tagging will be skipped if this value is missing._
	* `GIT_PASSWORD` - The password to be used to tag the source code at `origin` with the current build number. For production builds only. _Tagging will be skipped if this value is missing._
	* `NUGET_APIKEY` - The API key to be used to publish NuGet packages to the [official feed](https://www.nuget.org/). _Publishing will fail if this value is missing._
* `DotNet.cake` - Contains the main task definitions for working with .NET projects. There are some meta task definitions at the top that are meant to be the primary tasks.
* `Git.cake` - Contains some task definitions related to working with Git repositories.

You can include these Cake definitions in your repository by downloading the appropriate [Cake bootstrap file(s)](https://cakebuild.net/docs/tutorials/setting-up-a-new-project) and then including a simple `build.cake` file - somethig like this:
```
#load "./build/DotNet.cake"

var target = Argument("target", "CI");

RunTarget(target);
```

## [AppVeyor](https://www.appveyor.com/) Build Definitions

These files contains simple build definitions for use with [AppVeyor](https://www.appveyor.com/). They simply define the build environment and execute the appropriate [Cake](https://cakebuild.net/) tasks:

* `AppVeyor.CI.yml` - Contains a build definition for a standard [CI](https://en.wikipedia.org/wiki/Continuous_integration) build, including:
	* Building the code in `Debug` mode
	* Running any automated tests.
* `AppVeyor.Release.yml` - Contains a build definition for a standard build for production deployment, including:
	* Building the code in `Release` mode
	* Building any NuGet packages
	* Tagging the source code at `origin` with the build version number
	* Publishing any NuGet packages to the [official feed](https://www.nuget.org/)

You can apply these build definitions by simply creating a new build definition in [AppVeyor](https://www.appveyor.com/), naming it, and pointing it to the appropriate raw file on GitHub.

For CI builds:
```
https://raw.githubusercontent.com/TaffarelJr/OnionSeed.Build/master/AppVeyor.CI.yml
```

... and for Release builds:
```
https://raw.githubusercontent.com/TaffarelJr/OnionSeed.Build/master/AppVeyor.Release.yml
```
