using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class StatusCenter : UserControl
    {
        public static ObservableCollection<StatusBanner> StatusBannersSource { get; set; } = new ObservableCollection<StatusBanner>();

        public StatusCenter()
        {
            this.InitializeComponent();
        }

        public void RemoveBanner(StatusBanner banner)
        {
            StatusBannersSource.Remove(banner);
        }

        public StatusBanner PostBanner(string title, string message, uint initialProgress, StatusBanner.StatusBannerSeverity severity, StatusBanner.StatusBannerOperation operation)
        {
            var item = new StatusBanner(message, title, initialProgress, severity, operation);
            StatusBannersSource.Add(item);
            return item;
        }
    }

    public class StatusBanner : ObservableObject, IProgress<uint>
    {
        private string _FullTitle;
        private uint Progress { get; set; } = 0;

        public bool IsProgressing { get; } = false;
        public string Title { get; }
        public StatusBannerSeverity Severity { get; } = StatusBannerSeverity.Ongoing;
        public StatusBannerOperation Operation { get; }
        public string Message { get; }
        public SolidColorBrush StrokeColor { get; } = new SolidColorBrush(Colors.DeepSkyBlue);
        public IconSource GlyphSource { get; }

        public string FullTitle
        {
            get => _FullTitle;
            set => SetProperty(ref _FullTitle, value);
        }

        public enum StatusBannerSeverity
        {
            Ongoing,
            Success,
            Error
        }

        public enum StatusBannerOperation
        {
            Recycle,
            Delete,
            Paste,
            Extract
        }

        public StatusBanner(string message, string title, uint progress, StatusBannerSeverity severity, StatusBannerOperation operation)
        {
            Message = message;
            Title = title;
            Progress = progress;
            Severity = severity;
            Operation = operation;

            switch (Severity)
            {
                case StatusBannerSeverity.Ongoing:
                    IsProgressing = true;
                    if (string.IsNullOrWhiteSpace(Title))
                    {
                        switch (Operation)
                        {
                            case StatusBannerOperation.Extract:
                                Title = ResourceController.GetTranslation("ExtractInProgress/Title");
                                GlyphSource = new FontIconSource()
                                {
                                    Glyph = "\xED25"    // OpenFolder to substitute for extract glyph
                                };
                                break;

                            case StatusBannerOperation.Paste:
                                Title = ResourceController.GetTranslation("PasteInProgress/Title");
                                GlyphSource = new FontIconSource()
                                {
                                    Glyph = "\xE77F"    // Paste glyph
                                };
                                break;

                            case StatusBannerOperation.Delete:
                                Title = ResourceController.GetTranslation("DeleteInProgress/Title");
                                GlyphSource = new FontIconSource()
                                {
                                    Glyph = "\xE74D"    // Delete glyph
                                };
                                break;

                            case StatusBannerOperation.Recycle:
                                Title = ResourceController.GetTranslation("RecycleInProgress/Title");
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["RecycleBinIcons"] as FontFamily,
                                    Glyph = "\xEF87"    // RecycleBin Custom Glyph
                                };
                                break;
                        }
                    }
                    FullTitle = Title + " (" + Progress + "%)";
                    break;

                case StatusBannerSeverity.Success:
                    if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        FullTitle = Title;
                        StrokeColor = new SolidColorBrush(Colors.Green);
                        GlyphSource = new FontIconSource()
                        {
                            Glyph = "\xE73E"    // CheckMark glyph
                        };
                    }
                    break;

                case StatusBannerSeverity.Error:
                    if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        FullTitle = Title;
                        StrokeColor = new SolidColorBrush(Colors.Red);
                        GlyphSource = new FontIconSource()
                        {
                            Glyph = "\xE783"    // Error glyph
                        };
                    }
                    break;
            }
        }

        public void Report(uint value)
        {
            if (value <= 100)
            {
                Progress = value;
                FullTitle = Title + " (" + Progress + "%)";
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}