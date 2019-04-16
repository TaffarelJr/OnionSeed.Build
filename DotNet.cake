#load "./Constants.cake"
#load "./Git.cake"

Task("BuildFromScratch")
	.IsDependentOn("PrintDotNetVersion")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("SetVersion")
	.IsDependentOn("Build");

Task("CI")
	.IsDependentOn("BuildFromScratch")
	.IsDependentOn("Test");

Task("ProductionBuild")
	.IsDependentOn("SetReleaseConfig")
	.IsDependentOn("BuildFromScratch")
	.IsDependentOn("Pack")
	.IsDependentOn("TagBuild");

Task("SetReleaseConfig")
	.Does(() =>
	{
		var config = "Release";
		Information($"Configuration set to '{config}'");
		Constants.Build.Configuration = config;
	});

Task("PrintDotNetVersion")
	.Does(() =>
	{
		StartProcess("dotnet", new ProcessSettings
		{
			Arguments = new ProcessArgumentBuilder()
				.Append("--version")
		});
	});

Task("Clean")
	.Does(() =>
	{
		DotNetCoreClean(".", new DotNetCoreCleanSettings
		{
			Configuration = Constants.Build.Configuration,
			Verbosity = DotNetCoreVerbosity.Minimal
		});
	});

Task("Restore")
	.Does(() =>
	{
		DotNetCoreRestore(new DotNetCoreRestoreSettings
		{
			Sources = Constants.NuGet.DependencyFeeds,
			Verbosity = DotNetCoreVerbosity.Minimal
		});
	});

Task("SetVersion")
	.Does(() =>
	{
		Constants.Build.Version = EnvironmentVariable(Constants.EnvironmentVariables.Version) ?? Constants.Build.DefaultVersion;
		Information($"Building version {Constants.Build.Version}");
	});

Task("Build")
	.Does(() =>
	{
		DotNetCoreBuild(".", new DotNetCoreBuildSettings
		{
			Configuration = Constants.Build.Configuration,
			MSBuildSettings = new DotNetCoreMSBuildSettings()
				.SetVersion(Constants.Build.Version),
			NoRestore = true,
			Verbosity = DotNetCoreVerbosity.Minimal
		});
	});

Task("Test")
	.DoesForEach(GetSubDirectories("./test"), project =>
	{
		DotNetCoreTest(project.FullPath, new DotNetCoreTestSettings
		{
			Configuration = Constants.Build.Configuration,
			NoBuild = true,
			NoRestore = true,
			Verbosity = DotNetCoreVerbosity.Normal
		});
	});

Task("Pack")
	.DoesForEach(GetSubDirectories("./src"), project =>
	{
		DotNetCorePack(project.FullPath, new DotNetCorePackSettings
		{
			Configuration = Constants.Build.Configuration,
			MSBuildSettings = new DotNetCoreMSBuildSettings()
				.SetVersion(Constants.Build.Version),
			NoBuild = true,
			NoRestore = true,
			Verbosity = DotNetCoreVerbosity.Normal
		});
	});

Task("Push")
	.Does(() =>
	{
		var apiKey = EnvironmentVariable(Constants.EnvironmentVariables.NuGetApiKey);
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new InvalidOperationException("NuGet API key was not found");

		foreach (var package in GetFiles("./src/**/*.nupkg"))
		{
			DotNetCoreNuGetPush(package.FullPath, new DotNetCoreNuGetPushSettings
			{
				ApiKey = apiKey,
				Source = Constants.NuGet.PublishRoot
			});
		}
	});
