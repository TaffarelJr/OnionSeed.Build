#addin Cake.Npm&version=0.17.0

Task("npm-install")
    .DoesForEach(() => Build.Spas, project =>
    {
        Information($"Running 'npm install' in working directory '{project.Path}' ...");
        NpmInstall(new NpmInstallSettings
        {
            LogLevel = NpmLogLevel.Warn,
            WorkingDirectory = project.Path
        });
    });

Task("npm-test")
    .DoesForEach(() => Build.Spas, project =>
    {
        Information($"Running 'npm test' in working directory '{project.Path}' ...");
        NpmRunScript(new NpmRunScriptSettings
        {
            ScriptName = "test",
            WorkingDirectory = project.Path
        });
    });
