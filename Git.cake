public static class Git
{
    public static readonly string Username = Environment.GetEnvironmentVariable("GIT_USERNAME");
    public static readonly string Password = Environment.GetEnvironmentVariable("GIT_PASSWORD");

    public static string Execute(string arguments)
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });

        return process.StandardOutput.ReadToEnd().Trim();
    }

    public static string CurrentBranch()
    {
        return Execute("rev-parse --abbrev-ref HEAD");
    }

    public static string GetOriginUri()
    {
        return Execute("config --get remote.origin.url");
    }

    public static string GetOriginUriWithoutSuffix()
    {
        var uri = GetOriginUri();

        var index = uri.LastIndexOf(".git");
        if (index >= 0)
            uri = uri.Remove(index);

        return uri;
    }
}

public static bool IsMaster(this string branch)
{
    return branch.Equals("master", StringComparison.OrdinalIgnoreCase);
}

Task("git-tag-build")
    .WithCriteria(!BuildSystem.IsLocalBuild) // Only during CI/CD
    .Does(() =>
    {
        Information("Adding tag to git repo ...");
        if (string.IsNullOrWhiteSpace(Git.Username))
        {
            Information($"Git username was not found, tag could not be created.");
        }
        else if (string.IsNullOrWhiteSpace(Git.Password))
        {
            Information($"Git password was not found, tag could not be created.");
        }
        else
        {
            Information(Git.Execute($"tag {Build.Version}"));

            var origin = new Uri(Git.GetOriginUri());
            var username = System.Net.WebUtility.UrlEncode(Git.Username);
            var password = System.Net.WebUtility.UrlEncode(Git.Password);
            var uri = $"http://{username}:{password}@{origin.Host}{origin.PathAndQuery}";

            Information($"Pushing tag '{Build.Version}' to '{origin}' ...");
            Information(Git.Execute($"push {uri} {Build.Version}"));
        }
    });
