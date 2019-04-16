public static class Git {
	public static string CurrentBranch()
	{
		return Execute("rev-parse --abbrev-ref HEAD");
	}

	public static bool CurrentBranchIsMaster()
	{
		return CurrentBranch().Equals("master", StringComparison.OrdinalIgnoreCase);
	}

	public static string Execute(string arguments)
	{
		var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
		{
			FileName = "git",
			Arguments = arguments,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			CreateNoWindow = true
		});

		return git.StandardOutput.ReadToEnd().Trim();
	}
}

Task("TagBuild")
	.Does(() =>
	{
		var username = EnvironmentVariable(Constants.EnvironmentVariables.GitUsername);
		var password = EnvironmentVariable(Constants.EnvironmentVariables.GitPassword);
		password = System.Net.WebUtility.UrlEncode(password);

		var origin = new Uri(Git.Execute("config --get remote.origin.url"));
		var uri = $"https://{username}:{password}@{origin.Host}{origin.PathAndQuery}";

		if (!string.IsNullOrWhiteSpace(username) &&
			!string.IsNullOrWhiteSpace(password))
		{
			Information("Settings found, creating tag ...");
			var result = StartProcess("git", new ProcessSettings
			{
				Arguments = new ProcessArgumentBuilder()
					.Append("tag")
					.Append(Constants.Build.Version)
			});

			if (result != 0)
			{
				Information($"Tag failed. Result: {result}");
				return;
			}

			Information("Tag successful. Pushing to origin ...");
			result = StartProcess("git", new ProcessSettings
			{
				Arguments = new ProcessArgumentBuilder()
					.Append("push")
					.Append(uri)
					.Append(Constants.Build.Version)
			});

			if (result != 0)
			{
				Information($"Push failed. Result: {result}");
				return;
			}

			Information("Push successful.");
		}
	});
