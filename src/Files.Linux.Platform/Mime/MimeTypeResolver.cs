namespace Files.Linux.Platform.Mime;

/// <summary>
/// Resolves MIME types for files on Linux.
/// Uses freedesktop.org shared-mime-info database.
/// </summary>
public class MimeTypeResolver
{
    private static readonly Dictionary<string, string> ExtensionMimeMap = new()
    {
        // Documents
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".txt", "text/plain" },
        { ".rtf", "application/rtf" },

        // Spreadsheets
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".csv", "text/csv" },

        // Presentations
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

        // Images
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".svg", "image/svg+xml" },
        { ".webp", "image/webp" },

        // Audio
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".flac", "audio/flac" },
        { ".ogg", "audio/ogg" },
        { ".m4a", "audio/mp4" },

        // Video
        { ".mp4", "video/mp4" },
        { ".mkv", "video/x-matroska" },
        { ".webm", "video/webm" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },

        // Archives
        { ".zip", "application/zip" },
        { ".tar", "application/x-tar" },
        { ".gz", "application/gzip" },
        { ".rar", "application/x-rar-compressed" },
        { ".7z", "application/x-7z-compressed" },

        // Code
        { ".cs", "text/plain" },
        { ".py", "text/plain" },
        { ".js", "text/javascript" },
        { ".html", "text/html" },
        { ".json", "application/json" },
    };

    /// <summary>
    /// Gets MIME type for a file path.
    /// </summary>
    public static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (ExtensionMimeMap.TryGetValue(extension, out var mimeType))
            return mimeType;

        // Try using file command
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "file",
                Arguments = $"--mime-type -b \"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                if (!string.IsNullOrEmpty(output))
                    return output;
            }
        }
        catch
        {
            // Fallback to default
        }

        return "application/octet-stream";
    }

    /// <summary>
    /// Gets human-readable description for MIME type.
    /// </summary>
    public static string GetMimeDescription(string mimeType)
    {
        return mimeType switch
        {
            "application/pdf" => "PDF Document",
            "application/zip" => "ZIP Archive",
            "text/plain" => "Text File",
            "image/jpeg" => "JPEG Image",
            "image/png" => "PNG Image",
            "audio/mpeg" => "MP3 Audio",
            "video/mp4" => "MP4 Video",
            _ => mimeType
        };
    }
}