public static class DotNet {
	public static string Execute(string arguments)
	{
		var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = arguments,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			CreateNoWindow = true
		});

		return process.StandardOutput.ReadToEnd().Trim();
	}
}

Task("dotnet-version")
	.Does(() =>
	{
		Information($".NET Core version:  {DotNet.Execute("--version")}");
		Information($"Code build:         {Build.Version}");
	});

Task("dotnet-clean")
	.Does(() =>
	{
		DotNetCoreClean(".", new DotNetCoreCleanSettings
		{
			Configuration = Build.Configuration,
			Verbosity = DotNetCoreVerbosity.Minimal
		});
	});

Task("dotnet-restore")
	.Does(() =>
	{
		Information(NuGet.Execute("sources list"));
		Information("");
		DotNetCoreRestore(new DotNetCoreRestoreSettings
		{
			Verbosity = DotNetCoreVerbosity.Minimal
		});
	});

Task("dotnet-build")
	.Does(() =>
	{
		Information($"Building version {Build.Version} ...");
		DotNetCoreBuild(".", new DotNetCoreBuildSettings
		{
			Configuration = Build.Configuration,
			MSBuildSettings = new DotNetCoreMSBuildSettings()
				.SetVersion(Build.Version),
			NoRestore = true,
			Verbosity = DotNetCoreVerbosity.Minimal
		});
	});

Task("dotnet-unit-tests")
	.DoesForEach(() => Build.UnitTests, project =>
	{
		Information($"Running unit tests in '{project.Path}' ...");
		DotNetCoreTest(project.Path, new DotNetCoreTestSettings
		{
			Configuration = Build.Configuration,
			NoBuild = true,
			NoRestore = true,
			Verbosity = DotNetCoreVerbosity.Normal,
			ArgumentCustomization = args => args
				.Append("/p:CollectCoverage=true")
				.Append("/p:CoverletOutput=TestResults/")
				.Append("/p:CoverletOutputFormat=lcov")
		});
	});

Task("dotnet-integration-tests")
	.DoesForEach(() => Build.IntegrationTests, project =>
	{
		Information($"Running integration tests in '{project.Path}' ...");

		DotNetCoreTest(project.Path, new DotNetCoreTestSettings
		{
			Configuration = Build.Configuration,
			NoBuild = true,
			NoRestore = true,
			Verbosity = DotNetCoreVerbosity.Normal
		});
	});

Task("dotnet-publish")
	.Does(() =>
	{
		Information($"Deleting folder '{Build.Folders.Publish}' ...");
		if (DirectoryExists(Build.Folders.Publish))
			DeleteDirectory(Build.Folders.Publish, new DeleteDirectorySettings
			{
				Recursive = true
			});
	})
	.DoesForEach(() => Build.ToBePublished, project =>
	{
		var output = System.IO.Path.Combine(Build.Folders.Publish, project.Name);
		Information($"Publishing project '{project.Path}' to '{output}'...");

		DotNetCorePublish(project.Path, new DotNetCorePublishSettings
		{
			Configuration = Build.Configuration,
			DiagnosticOutput = false,
			NoBuild = true,
			NoRestore = true,
			OutputDirectory = output,
			Verbosity = DotNetCoreVerbosity.Normal
		});
	});
