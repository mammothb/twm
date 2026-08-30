namespace Twm.Platform.Config;

/// <summary>Where Twm looks for its config file.</summary>
public static class ConfigPaths
{
    public const string DirectionName = ".twm";
    public const string FileName = "config.yaml";

    /// <summary>
    /// The default path: <c>%USERPROFILE%/.twm/config.yaml</c> (user's home on
    /// any OS).
    /// </summary>
    public static string Default()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, DirectionName, FileName);
    }
}
