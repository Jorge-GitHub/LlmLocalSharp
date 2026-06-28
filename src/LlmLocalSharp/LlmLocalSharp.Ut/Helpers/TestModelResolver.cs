namespace LlmLocalSharp.Ut.Helpers;

/// <summary>
/// Resolves model file paths for integration tests.
/// Locates the repository root by walking up from the test output directory
/// and looking for the <c>.git</c> folder, then resolves model paths
/// relative to the <c>models/</c> subfolder.
/// </summary>
internal static class TestModelResolver
{
    private static readonly string ModelsDirectory = ResolveModelsDirectory();

    /// <summary>
    /// Resolves the full path to a model file inside the <c>models/</c> folder.
    /// Skips the test with <see cref="Assert.Inconclusive"/> if the file is not found.
    /// </summary>
    public static string GetModelPath(string fileName)
    {
        string fullPath = Path.Combine(ModelsDirectory, fileName);

        if (!File.Exists(fullPath))
        {
            Assert.Inconclusive(
                $"Model file not found: {fullPath}. " +
                $"Download the model into the 'models/' folder at the repository root to run this test.");
        }

        return fullPath;
    }

    /// <summary>
    /// Walks up from the test output directory to find the repository root
    /// (identified by the <c>.git</c> folder), then returns the <c>models/</c> subfolder path.
    /// </summary>
    private static string ResolveModelsDirectory()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return Path.Combine(directory.FullName, "models");
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "models");
    }
}
