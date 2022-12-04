using ABI.System;
using Files.Shared.Enums;

namespace Files.App.Filesystem
{
    public struct FileSystemProgress
    {
        public float Progress { get; set; }
        public FileSystemStatusCode? Status { get; set; }
        public string? FileName { get; set; }
        public long? TotalSize { get; set; }
        public long? ProcessedSize { get; set; }
        public long? FileCount { get; set; }
        public long? ProcessedFileCount { get; set; }
        public long? DirectoryCount { get; set; }
        public long? ProcessedDirectoryCount { get; set; }
        public float? ItemSpeed { get; set; }
        public float? SizeSpeed { get; set; }
        public TimeSpan EstimatedArrivalTime { get; set; }
        public TimeSpan CostedTime { get; set; }

        public static implicit operator FileSystemProgress(float progress)
        {
            return new() { Progress = progress };
        }

        public static implicit operator FileSystemProgress(FileSystemStatusCode status)
        {
            return new() { Status = status };
        }
    }
}
