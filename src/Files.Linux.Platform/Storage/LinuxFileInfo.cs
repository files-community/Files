namespace Files.Linux.Platform.Storage;

public class LinuxFileInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime Modified { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsHidden { get; set; }

    public string Extension => Path.GetExtension(Name);
}