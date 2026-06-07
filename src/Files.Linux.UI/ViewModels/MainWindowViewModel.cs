using System.Collections.ObjectModel;
using Files.Linux.UI.Models;

namespace Files.Linux.UI.ViewModels;

public class MainWindowViewModel
{
    public ObservableCollection<SidebarItem> SidebarItems { get; }
    public ObservableCollection<FileItem> Files { get; }
    
    private string? _selectedPath;
    public string? SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (_selectedPath != value)
            {
                _selectedPath = value;
                LoadFilesForPath(value);
            }
        }
    }

    public MainWindowViewModel()
    {
        SidebarItems = new ObservableCollection<SidebarItem>
        {
            new SidebarItem { Name = "Home", Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
            new SidebarItem { Name = "Desktop", Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
            new SidebarItem { Name = "Documents", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
            new SidebarItem { Name = "Downloads", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") },
            new SidebarItem { Name = "Root", Path = "/" }
        };

        Files = new ObservableCollection<FileItem>();
        SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private void LoadFilesForPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        Files.Clear();

        try
        {
            var directoryInfo = new DirectoryInfo(path);
            
            // Add directories first
            foreach (var dir in directoryInfo.GetDirectories())
            {
                Files.Add(new FileItem
                {
                    Name = dir.Name,
                    Type = "Folder",
                    Modified = dir.LastWriteTime.ToString("g"),
                    Size = "-"
                });
            }

            // Then files
            foreach (var file in directoryInfo.GetFiles())
            {
                Files.Add(new FileItem
                {
                    Name = file.Name,
                    Type = Path.GetExtension(file.Name),
                    Modified = file.LastWriteTime.ToString("g"),
                    Size = FormatFileSize(file.Length)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading files: {ex.Message}");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}