public static class Constants
{
	public static class Build
	{
		public const string DefaultVersion = "1.0.0";

		public static string Version = DefaultVersion;
		public static string Configuration = "Debug";
	}

	public static class EnvironmentVariables
	{
		public const string GitPassword = "GIT_PASSWORD";
		public const string GitUsername = "GIT_USERNAME";
		public const string NuGetApiKey = "NUGET_APIKEY";
		public const string Version = "APPVEYOR_BUILD_VERSION";
	}

	public static class NuGet
	{
		public readonly static string[] DependencyFeeds =
		{
			"https://api.nuget.org/v3/index.json"
		};

		public const string PublishRoot = "https://www.nuget.org/api/v2/package/";
	}
}
