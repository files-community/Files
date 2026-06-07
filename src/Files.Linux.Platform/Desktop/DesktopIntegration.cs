namespace Files.Linux.Platform.Desktop;

/// <summary>
/// Handles Linux desktop environment integration.
/// Supports freedesktop.org standards and common DE conventions.
/// </summary>
public class DesktopIntegration
{
    /// <summary>
    /// Gets the current desktop environment.
    /// </summary>
    public static string? GetDesktopEnvironment()
    {
        var xdgSessionDesktop = Environment.GetEnvironmentVariable("XDG_SESSION_DESKTOP");
        var xdgCurrentDesktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
        var desktopSession = Environment.GetEnvironmentVariable("DESKTOP_SESSION");

        return xdgCurrentDesktop ?? xdgSessionDesktop ?? desktopSession ?? "unknown";
    }

    /// <summary>
    /// Opens a file with the default application.
    /// </summary>
    public static async Task<bool> OpenFileAsync(string filePath)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = filePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            await (process?.WaitForExitAsync() ?? Task.CompletedTask);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Copies a file to clipboard.
    /// </summary>
    public static async Task<bool> CopyToClipboardAsync(string filePath)
    {
        try
        {
            // Try using xclip
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "xclip",
                Arguments = $"-selection clipboard -t text/plain -i",
                UseShellExecute = false,
                RedirectStandardInput = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                process.StandardInput.WriteLine(filePath);
                process.StandardInput.Close();
                await process.WaitForExitAsync();
                return true;
            }
        }
        catch
        {
            // Fallback to xsel if xclip is not available
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xsel",
                    Arguments = "-b -i",
                    UseShellExecute = false,
                    RedirectStandardInput = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    process.StandardInput.WriteLine(filePath);
                    process.StandardInput.Close();
                    await process.WaitForExitAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the theme/appearance settings.
    /// </summary>
    public static string GetThemePreference()
    {
        // Try to read GTK theme setting
        var gtkSettingsIni = Path.Combine(
            Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"),
            "gtk-3.0", "settings.ini");

        try
        {
            if (File.Exists(gtkSettingsIni))
            {
                var content = File.ReadAllText(gtkSettingsIni);
                if (content.Contains("gtk-application-prefer-dark-theme=true"))
                    return "dark";
            }
        }
        catch
        {
            // Ignore errors reading config
        }

        return "light";
    }
}