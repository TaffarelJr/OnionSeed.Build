# OnionSeed.Build

Contains a set of files intended to reduce boilerplate when setting up a new [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/) solution and its associated build/[CI](https://en.wikipedia.org/wiki/Continuous_integration) process.

## Status

| Work in Progress                                                                                                                                                 |                                                                                                                                                        |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| [![GitHub pull requests](https://img.shields.io/github/issues-pr-raw/TaffarelJr/OnionSeed.Build.svg?logo=github)](https://github.com/TaffarelJr/OnionSeed.Build) | [![GitHub issues](https://img.shields.io/github/issues-raw/TaffarelJr/OnionSeed.Build.svg?logo=github)](https://github.com/TaffarelJr/OnionSeed.Build) |

## Common [MSBuild Project](https://docs.microsoft.com/en-us/aspnet/web-forms/overview/deployment/web-deployment-in-the-enterprise/understanding-the-project-file) Property Files

These property files are designed to encapsulate some of the standard settings that should be included in `.csproj` files.

### `Common.props`

This file contains general settings intended for all Visual Studio projects. It includes a reference to `StyleCop.json`, which identifies the [StyleCop](https://github.com/StyleCop/StyleCop) settings to be applied.

### `Production.props`

This file builds on `Common.props`, and contains additional settings specifically for production code. It includes a reference to `Common.props`, so it's automatically included; it also includes a reference to `Production.ruleset`, which identifies the (more stringent) [CodeAnalysis](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) rules that should be applied to production code.

These settings should be applied to deployable `.csproj` files using the following code (modifying the path if necessary):

```xml
<Project Sdk="Microsoft.NET.Sdk">
	...
	<Import Project="..\..\build\Production.props" />
	...
</Project>
```

### `Test.props`

This file builds on `Common.props`, and contains additional settings specifically for test projects. It includes a reference to `Common.props`, so it's automatically included; it also includes a reference to `Test.ruleset`, which identifies the (more lenient) [CodeAnalysis](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) rules that should be applied to test code.

These settings should be applied to test `.csproj` files using the following code (modifying the path if necessary):

```xml
<Project Sdk="Microsoft.NET.Sdk">
	...
	<Import Project="..\..\build\Test.props" />
	...
</Project>
```

## Common [Cake](https://cakebuild.net/) Task Definitions

These files encapsulate a standard .NET build/test/deploy process, and can be re-used for any .NET solution. They can be executed locally as well as on a build server, and should perform identically in each location. The idea behind this is to have the build process under source control (instead of implemented in various UI settings on a build server) and also so these same scripts don't have to constantly be recreated for each new .NET solution.

To make use of these Cake tasks in a .NET solution:

1. [Download the latest Cake bootstrap script(s)](https://cakebuild.net/docs/tutorials/setting-up-a-new-project) to the root project folder, and
2. Create a `build.cake` file in the root folder containing the default settings and any project-specific configuration. Configuration is specified in a fluent style:

	```csharp
	// Import the Main Cake file - this includes all the other common Cake files
	#load "build\Main.cake"

	// Set which Cake task you want to be the default
	var target = Argument("target", "ci");

	// Set up project-specific configuration as below:

	FromSourceFolder("ClassLibrary")    // .\src\ClassLibrary\
	    .DeployNuGetPackage();          // Configures the project to be deployed as a NuGet package

	FromSourceFolder("ApiSite")         // .\src\ApiSite\
	    .Publish();                     // Publishes the website to .\publish\ApiSite\

	FromSourceFolder("ReactSite")       // .\src\ReactSite\
	    .WithSpa("ClientApp")           // Runs npm scripts in the working directory .\src\ReactSite\ClientApp\
	    .Publish();                     // Publishes the website to .\publish\ReactSite\

	FromTestFolder("UnitTests")         // .\test\UnitTests\
	    .AddUnitTests();                // Run the project as unit tests (short, quick, often)

	FromTestFolder("IntegrationTests")  // .\test\IntegrationTests\
	    .AddIntegrationTests();         // Run the project as integration tests (long-running, only when needed)

	RunTarget(target);  // Launch specified Cake task
	```

The following are descriptions of the various Cake files and their contents.

### DotNet.cake

This file contains all the task definitions and code necessary to run [`dotnet` commands](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet).

Task definitions include:

- `dotnet-version` - Displays the version of .NET Core installed on the current machine, as well as the version of the software being built.
- `dotnet-clean` - Cleans the current Visual Studio solution.
- `dotnet-restore` - Restores NuGet packages for the current Visual Studio solution.
- `dotnet-build` - Builds the current Visual Studio solution.
- `dotnet-unit-tests` - Runs the automated tests in all projects configured for unit tests. These are intended to be short tests, so they can be run often.
- `dotnet-integration-tests` - Runs the automated tests in all projects configured for integration tests. This is intended for longer-running tests, so they are only run when necessary.
- `dotnet-publish` - Publishes all projects configured for such. This is intended for web applications - so they can generate their final, optimized form.

Static functions include:

- `string DotNet.Execute(string arguments)` - Calls the `dotnet` command-line executable, passing in the given arguments, and returns the standard output as a `string`.

### Git.cake

This file contains all the task definitions and code necessary to run [`git` commands](https://git-scm.com/docs).

Task definitions include:

- `git-tag-build` - Tags the current commit in GitHub with the current build number. This is intended to be run after code has successfully been built and tested, and is ready to be deployed. _Only runs on a build server_.

Static functions include:

- `string Git.CurrentBranch()` - Returns the name of the current Git branch.
- `string Git.Execute(string arguments)` - Calls the `git` command-line executable, passing in the given arguments, and returns the standard output as a `string`.
- `string Git.GetOriginUri()` - Returns the URI of the current Git remote.
- `string Git.GetOriginUriWithoutSuffix()` - Returns the URI of the current Git remote, without the `.git` suffix.

Extension methods include:

- `bool IsMaster(this string branch)` - Returns `true` if the current Git branch is `master`; otherwise, `false`.

Environment variables include:

- `GIT_USERNAME` - The username to be used to tag the source code at `origin` with the current build number. _Only runs in `Release` builds on a build server. Tagging will be skipped if this value is missing._
- `GIT_PASSWORD` - The password to be used to tag the source code at `origin` with the current build number. _Only runs in `Release` builds on a build server. Tagging will be skipped if this value is missing._

### Main.cake

This is the main entry point for all the common Cake tasks; it contains several shared C# definitions and extension methods, as well as the main meta task definitions. It loads all the other Cake files, so this is the only file that needs to be referenced.

Task definitions include:

- `config` - Displays the local build configuration. This helps with debugging.
- `set-release-config` - Sets the build mode to `Release` for any following tasks. This should be at the beginning of a production-ready CI/CD run.
- `build` - _Meta task._ Completely builds the code.
- `test` - _Meta task._ Runs all unit tests on already-built code.
- `ci` - _Meta task._ Runs the entire CI process (`build`, `test`, then `dotnet-integration-tests`). _This is intended to be the default task, both locally and on the build server._
- `cd` - _Meta task._ Runs the entire CD process after CI is complete.
- `release` - _Meta task._ Runs a full `Release` build, suitable for production (`set-release-config`, `ci`, then `cd`).

Class definitions include:

- `Build` - A static class that contains current build information, such as `Version`, `Configuration`, and the map of which steps apply to which projects.
- `Build.Folders` - A static nested class that contains the conventional .NET Core folder names as constants.
- `Project` - Defines a project in the build configuration, consisting of both a name and a folder path.

Extension methods include:

- `Project FromFolder(this string name, string folder)` - Creates a new `Project` instance, referencing a project named `<name>` located at `<folder>\<name>\`.
- `Project FromSourceFolder(this string name)` - Creates a new `Project` instance, referencing a project named `<name>` located at `src\<name>\`.
- `Project FromTestFolder(this string name)` - Creates a new `Project` instance, referencing a project named `<name>` located at `test\<name>\`.
- `Project FromPublishFolder(this string name)` - Creates a new `Project` instance, referencing a project named `<name>` located at `publish\<name>\`.
- `Project AddUnitTests(this Project project)` - Adds the given `Project` to the list of projects to be run during unit testing, and returns the given `Project`.
- `Project AddIntegrationTests(this Project project)` - Adds the given `Project` to the list of projects to be run during integration testing, and returns the given `Project`.
- `Project AddSpa(this Project project, string spaDirectory)` - Adds the specified subdirectory inside the given `Project` to the list of projects to have NPM operations performed on them, and returns the given `Project`.
- `Project Publish(this Project project)` - Adds the given `Project` to the list of projects to be published, and returns a new `Project` instance pointing to the output located at `publish\<name>\`.
- `Project DeployNuGetPackage(this Project project)` - Adds the given `Project` to the list of NuGet packages to be created and pushed to a package server, and returns a new `Project` instance pointing to the output NuGet package located at `publish\<name>.<version>.nupkg`

Environment variables include:

- `BUILD_NUMBER` - The full build number. This will be used to build all the code and NuGet packages. If missing, the default is `1.0.0`. If building a branch that is not `master`, the branch name will be appended to the end of the buid number.

### Npm.cake

This file contains all the task definitions and code necessary to run [`npm` commands](https://docs.npmjs.com/cli-documentation/cli).

Task definitions include:

- `npm-install` - Restores all necessary NPM packages as configured.
- `npm-test` - Runs any automated NPM test scripts as configured.

### NuGet.cake

This file contains all the task definitions and code necessary to run [`nuget` commands](https://docs.microsoft.com/en-us/nuget/reference/nuget-exe-cli-reference).

Task definitions include:

- `nuget-pack` - Creates NuGet packages for all projects configured to be deployed only as NuGet packages.
- `nuget-push` - Deploys ALL NuGet packages to the Artifactory server. _Only runs on a build server._

Static methods include:

- `string NuGet.GetPackagePath(string name)` - Gets the relative path to the NuGet package with the given name, using the current build version number, in the `publish` directory.

Static properties include:

- `string NuGet.PublishRootUri` - The `nuget-push` task uses this to calculate the URI for the channel in Artifactory where any NuGet packages will be published, based on the current branch.

Environment variables include:

- `NUGET_EXE` - The path to the NuGet executable on the current system. This path is discovered, and the variable set, when Cake runs. No need to set it manually.
- `NUGET_BASEURL` - The URI of the package server where NuGet packages should be published. _If missing, the `nuget-push` task will throw an exception._
- `NUGET_APIKEY` - The API key used to publish NuGet packages to the package server. _If missing, the `nuget-push` task will throw an exception._






* `Constants.cake` - Contains some constant value definitions, including the names of the [environment variables](https://en.wikipedia.org/wiki/Environment_variable) that are required:
	* `APPVEYOR_BUILD_VERSION` - Contains the version number for the current build. _Defaults to `1.0.0` if this value is missing._

## [AppVeyor](https://www.appveyor.com/) Build Definitions

These files contains simple build definitions for use with [AppVeyor](https://www.appveyor.com/). They simply define the build environment and execute the appropriate [Cake](https://cakebuild.net/) tasks:

* `AppVeyor.CI.yml` - Contains a build definition for a standard [CI](https://en.wikipedia.org/wiki/Continuous_integration) build, including:
	* Building the code in `Debug` mode
	* Running any automated tests.
* `AppVeyor.Release.yml` - Contains a build definition for a standard build for production deployment, including:
	* Building the code in `Release` mode
	* Building any NuGet packages
	* Tagging the source code at `origin` with the build version number
	* Publishing any NuGet packages to a package server

You can apply these build definitions by simply creating a new build definition in [AppVeyor](https://www.appveyor.com/), naming it, and pointing it to the appropriate raw file on GitHub.

For CI builds:
```
https://raw.githubusercontent.com/TaffarelJr/OnionSeed.Build/master/AppVeyor.CI.yml
```

For Release builds:
```
https://raw.githubusercontent.com/TaffarelJr/OnionSeed.Build/master/AppVeyor.Release.yml
```
