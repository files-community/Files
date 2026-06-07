namespace Files.Linux.Platform.Utils;

/// <summary>
/// Linux-specific path utilities.
/// Handles standard Linux directory paths and conventions.
/// </summary>
public static class LinuxPaths
{
    /// <summary>
    /// Gets the home directory path.
    /// </summary>
    public static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// Gets the Desktop path.
    /// </summary>
    public static string Desktop => Path.Combine(Home, "Desktop");

    /// <summary>
    /// Gets the Documents path.
    /// </summary>
    public static string Documents => Path.Combine(Home, "Documents");

    /// <summary>
    /// Gets the Downloads path.
    /// </summary>
    public static string Downloads => Path.Combine(Home, "Downloads");

    /// <summary>
    /// Gets the Pictures path.
    /// </summary>
    public static string Pictures => Path.Combine(Home, "Pictures");

    /// <summary>
    /// Gets the Music path.
    /// </summary>
    public static string Music => Path.Combine(Home, "Music");

    /// <summary>
    /// Gets the Videos path.
    /// </summary>
    public static string Videos => Path.Combine(Home, "Videos");

    /// <summary>
    /// Gets the root directory.
    /// </summary>
    public static string Root => "/";

    /// <summary>
    /// Gets XDG config directory (~/.config).
    /// </summary>
    public static string Config => Path.Combine(Home, ".config");

    /// <summary>
    /// Gets XDG data directory (~/.local/share).
    /// </summary>
    public static string LocalShare => Path.Combine(Home, ".local", "share");

    /// <summary>
    /// Gets XDG cache directory (~/.cache).
    /// </summary>
    public static string Cache => Path.Combine(Home, ".cache");

    /// <summary>
    /// Normalizes path separators for Linux (uses /).
    /// </summary>
    public static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Checks if path is absolute.
    /// </summary>
    public static bool IsAbsolute(string path)
    {
        return Path.IsPathRooted(path);
    }

    /// <summary>
    /// Checks if path is a hidden file (starts with .).
    /// </summary>
    public static bool IsHidden(string path)
    {
        return Path.GetFileName(path).StartsWith('.');
    }
}