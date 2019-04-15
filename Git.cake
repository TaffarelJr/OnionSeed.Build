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
	.WithCriteria(Git.CurrentBranchIsMaster())
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
			StartProcess("git", new ProcessSettings
			{
				Arguments = new ProcessArgumentBuilder()
					.Append("tag")
					.Append(Constants.Build.Version)
			});

			StartProcess("git", new ProcessSettings
			{
				Arguments = new ProcessArgumentBuilder()
					.Append("push")
					.Append(uri)
					.Append(Constants.Build.Version)
			});
		}
	});