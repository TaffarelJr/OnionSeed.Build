#load "DotNet.cake"
#load "Git.cake"
#load "Npm.cake"
#load "NuGet.cake"

public static class Build
{
	public static class Folders
	{
		public const string Source = "src";
		public const string Test = "test";
		public const string Publish = "publish";
	}

	public static readonly string Version = $"{Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "1.0.0"}{GetBranchName()}";

	public static string Configuration { get; private set; } = "Debug";
	public static List<Project> UnitTests { get; set; } = new List<Project>();
	public static List<Project> IntegrationTests { get; set; } = new List<Project>();
	public static List<Project> Spas { get; set; } = new List<Project>();
	public static List<Project> ToBePublished { get; set; } = new List<Project>();
	public static List<Project> ToBePackedByNuGet { get; set; } = new List<Project>();
	public static List<Project> NuGetPackages { get; set; } = new List<Project>();

	public static void SetReleaseConfiguration() => Configuration = "Release";

	private static string GetBranchName()
	{
		var branch = Git.CurrentBranch();
		return branch.IsMaster()
			? string.Empty
			: $"-{branch}";
	}
}

public class Project
{
	public Project(string name, string path)
	{
		Name = name;
		Path = path;
	}

	public string Name { get; }
	public string Path { get; }
}

public static Project FromFolder(this string name, string folder) => new Project(name, System.IO.Path.Combine(folder, name));
public static Project FromSourceFolder(this string name) => name.FromFolder(Build.Folders.Source);
public static Project FromTestFolder(this string name) => name.FromFolder(Build.Folders.Test);
public static Project FromPublishFolder(this string name) => name.FromFolder(Build.Folders.Publish);

public static Project AddUnitTests(this Project project)
{
	Build.UnitTests.Add(project);
	return project;
}

public static Project AddIntegrationTests(this Project project)
{
	Build.IntegrationTests.Add(project);
	return project;
}

public static Project AddSpa(this Project project, string spaDirectory)
{
	Build.Spas.Add(new Project(project.Name, System.IO.Path.Combine(project.Path, spaDirectory)));
	return project;
}

public static Project Publish(this Project project)
{
	Build.ToBePublished.Add(project);
	return FromPublishFolder(project.Name);
}

public static Project DeployNuGetPackage(this Project project)
{
	Build.ToBePackedByNuGet.Add(project);
	var result = new Project(project.Name, NuGet.GetPackagePath(project.Name));
	Build.NuGetPackages.Add(result);
	return result;
}

Task("config")
	.Does(() =>
	{
		Information("\r\nUnit tests:");
		foreach (var project in Build.UnitTests)
			Information($"    - Name={project.Name}; Path={project.Path}");

		Information("\r\nIntegration tests:");
		foreach (var project in Build.IntegrationTests)
			Information($"    - Name={project.Name}; Path={project.Path}");

		Information("\r\nSPAs:");
		foreach (var project in Build.Spas)
			Information($"    - Name={project.Name}; Path={project.Path}");

		Information("\r\nTo be published:");
		foreach (var project in Build.ToBePublished)
			Information($"    - Name={project.Name}; Path={project.Path}");

		Information("\r\nTo be packed by NuGet:");
		foreach (var project in Build.ToBePackedByNuGet)
			Information($"    - Name={project.Name}; Path={project.Path}");

		Information("\r\nNuGet packages:");
		foreach (var project in Build.NuGetPackages)
			Information($"    - Name={project.Name}; Path={project.Path}");
	});

Task("set-release-config")
	.Does(() =>
	{
		Build.SetReleaseConfiguration();
		Information($"Configuration set to '{Build.Configuration}'");
	});

Task("build")
	.IsDependentOn("dotnet-version")
	.IsDependentOn("dotnet-clean")
	.IsDependentOn("dotnet-restore")
	.IsDependentOn("npm-install")
	.IsDependentOn("dotnet-build");

Task("test")
	.IsDependentOn("dotnet-unit-tests")
	.IsDependentOn("npm-test");

Task("ci")
	.IsDependentOn("build")
	.IsDependentOn("test")
	.IsDependentOn("dotnet-integration-tests");

Task("cd")
	.IsDependentOn("dotnet-publish")
	.IsDependentOn("git-tag-build")
	.IsDependentOn("nuget-pack")
	.IsDependentOn("nuget-push");

Task("release")
	.IsDependentOn("set-release-config")
	.IsDependentOn("ci")
	.IsDependentOn("cd");
