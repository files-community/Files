using Files.App.Helpers;
using Files.Shared.Enums;
using System;

namespace Files.App.Filesystem
{
    public class FileSystemProgress
    {
        private readonly IProgress<FileSystemProgress>? progress;
        private readonly IntervalSampler sampler;
        private FileSystemStatusCode? status;
        private bool criticalReport;
        private bool enumerationCompleted;

        public FileSystemStatusCode? Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    criticalReport = true;
                }
                status = value;
            }
        }
        public string? FileName { get; set; }
        public long TotalSize { get; set; }
        public long ProcessedSize { get; set; }
        public long ItemsCount { get; set; }
        public long ProcessedItemsCount { get; set; }
        public DateTimeOffset StartTime { get; }
        public bool EnumerationCompleted
        {
            get => enumerationCompleted;
            set
            {
                if (enumerationCompleted != value)
                {
                    criticalReport = true;
                }
                enumerationCompleted = value;
            }
        }

        public FileSystemProgress(IProgress<FileSystemProgress>? progress, int samplerInterval = 100)
        {
            this.StartTime = DateTimeOffset.Now;
            this.progress = progress;
            this.sampler = new(samplerInterval);
        }

        public void Report()
        {
            if (progress is not null && (criticalReport || sampler.CheckNow()))
            {
                progress.Report(this);
                criticalReport = false;
            }
        }

        public void ReportStatus(FileSystemStatusCode status)
        {
            Status = status;
            Report();
        }
    }
}
