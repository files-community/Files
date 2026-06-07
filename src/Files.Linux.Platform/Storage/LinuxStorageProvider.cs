using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Linux.Platform.Storage;

/// <summary>
/// Linux-specific implementation of storage provider.
/// Handles filesystem operations using Linux standard paths and permissions.
/// </summary>
public class LinuxStorageProvider
{
    private readonly string _basePath;

    public LinuxStorageProvider(string? basePath = null)
    {
        _basePath = basePath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    /// <summary>
    /// Gets all files in a directory.
    /// </summary>
    public async Task<IEnumerable<LinuxFileInfo>> GetFilesAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                var directory = new DirectoryInfo(path);
                if (!directory.Exists)
                    return Enumerable.Empty<LinuxFileInfo>();

                return directory.GetFiles()
                    .Select(f => new LinuxFileInfo
                    {
                        Name = f.Name,
                        FullPath = f.FullName,
                        Size = f.Length,
                        Modified = f.LastWriteTime,
                        IsDirectory = false,
                        IsHidden = f.Name.StartsWith('.')
                    })
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<LinuxFileInfo>();
            }
        });
    }

    /// <summary>
    /// Gets all directories in a path.
    /// </summary>
    public async Task<IEnumerable<LinuxFileInfo>> GetDirectoriesAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                var directory = new DirectoryInfo(path);
                if (!directory.Exists)
                    return Enumerable.Empty<LinuxFileInfo>();

                return directory.GetDirectories()
                    .Select(d => new LinuxFileInfo
                    {
                        Name = d.Name,
                        FullPath = d.FullName,
                        Size = 0,
                        Modified = d.LastWriteTime,
                        IsDirectory = true,
                        IsHidden = d.Name.StartsWith('.')
                    })
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<LinuxFileInfo>();
            }
        });
    }

    /// <summary>
    /// Gets all files and directories in a path.
    /// </summary>
    public async Task<IEnumerable<LinuxFileInfo>> GetEntriesAsync(string path, bool includeHidden = false)
    {
        var files = await GetFilesAsync(path);
        var directories = await GetDirectoriesAsync(path);

        var entries = files.Concat(directories)
            .Where(e => includeHidden || !e.IsHidden)
            .OrderByDescending(e => e.IsDirectory)
            .ThenBy(e => e.Name);

        return entries;
    }

    /// <summary>
    /// Creates a directory.
    /// </summary>
    public async Task<bool> CreateDirectoryAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Deletes a file or directory.
    /// </summary>
    public async Task<bool> DeleteAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Renames a file or directory.
    /// </summary>
    public async Task<bool> RenameAsync(string oldPath, string newPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath, false);
                    return true;
                }
                else if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Copies a file.
    /// </summary>
    public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false)
    {
        return await Task.Run(() =>
        {
            try
            {
                File.Copy(sourcePath, destinationPath, overwrite);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Checks if a path exists.
    /// </summary>
    public bool PathExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    /// <summary>
    /// Gets file permissions info.
    /// </summary>
    public async Task<LinuxFilePermissions> GetPermissionsAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                var fileInfo = new FileInfo(path);
                var attributes = fileInfo.Attributes;

                return new LinuxFilePermissions
                {
                    CanRead = (attributes & FileAttributes.ReadOnly) == 0 || true,
                    CanWrite = (attributes & FileAttributes.ReadOnly) == 0,
                    CanExecute = path.EndsWith(".sh") || path.EndsWith(".bin") || path.EndsWith(".out")
                };
            }
            catch
            {
                return new LinuxFilePermissions { CanRead = true };
            }
        });
    }
}