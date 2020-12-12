public static class NuGet
{
    public static readonly string NuGetExePath = Environment.GetEnvironmentVariable("NUGET_EXE");
    public static readonly string ServerUri = Environment.GetEnvironmentVariable("NUGET_BASEURL");
    public static readonly string ApiKey = Environment.GetEnvironmentVariable("NUGET_APIKEY");

    public static string GetPackagePath(string name)
    {
        var package = $"{name}.{Build.Version}.nupkg";
        return System.IO.Path.Combine(Build.Folders.Publish, package);
    }

    public static string Execute(string arguments)
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = NuGetExePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });

        return process.StandardOutput.ReadToEnd().Trim();
    }
}

Task("nuget-pack")
    .DoesForEach(() => Build.ToBePackedByNuGet, project =>
    {
        Information($"Creating NuGet package for '{project.Path}' in '{Build.Folders.Publish}'...");
        DotNetCorePack(project.Path, new DotNetCorePackSettings
        {
            Configuration = Build.Configuration,
            MSBuildSettings = new DotNetCoreMSBuildSettings()
                .SetVersion(Build.Version),
            NoBuild = true,
            NoRestore = true,
            OutputDirectory = Build.Folders.Publish,
            Verbosity = DotNetCoreVerbosity.Normal
        });
    });

Task("nuget-push")
    .WithCriteria(!BuildSystem.IsLocalBuild) // Only during CI/CD
    .Does(() =>
    {
        Information($"Checking variables for NuGet push ...");
        if (string.IsNullOrWhiteSpace(NuGet.ServerUri))
            throw new InvalidOperationException("NuGet artifact server uri environment variable was not found.");
        if (string.IsNullOrWhiteSpace(NuGet.ApiKey))
            throw new InvalidOperationException("NuGet API key environment variable was not found.");
    })
    .DoesForEach(() => Build.NuGetPackages, project =>
    {
        Information($"Pushing NuGet package '{project.Path}' to '{NuGet.ServerUri}' ...");
        DotNetCoreNuGetPush(project.Path, new DotNetCoreNuGetPushSettings
        {
            ApiKey = NuGet.ApiKey,
            Source = NuGet.ServerUri
        });
    });
